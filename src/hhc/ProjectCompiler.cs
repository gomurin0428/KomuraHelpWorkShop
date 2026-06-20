using System.Globalization;

namespace Komura.Hhc;

internal sealed class ProjectCompiler
{
    private readonly CliOptions _options;
    private readonly List<string> _warnings = new();

    public ProjectCompiler(CliOptions options)
    {
        _options = options;
    }

    public CompilationResult Compile()
    {
        var project = HhpProject.Load(_options.ProjectPath!);
        var flat = project.OptionIsYes("Flat");
        var lcid = TryParseLcid(project.Option("Language")) ?? CultureInfo.CurrentCulture.LCID;
        var helpTextEncoding = TextEncodingDetector.ForLcid(lcid);
        var files = CollectFiles(project, flat, helpTextEncoding);
        if (files.MissingRequired.Count > 0 && !_options.AllowMissing)
        {
            throw new CompilationException(
                "Missing required files: " + string.Join(", ", files.MissingRequired.Take(8))
                + (files.MissingRequired.Count > 8 ? " ..." : string.Empty));
        }

        WarnForUnsupportedOptions(project);

        var outputPath = ResolveOutputPath(project);
        var metadata = BuildMetadata(project, outputPath, lcid, helpTextEncoding, files.DefaultTopicArchivePath, files.GeneratedContentsFile is not null);
        var writer = new ChmWriter();
        writer.Write(outputPath, files.InputFiles, metadata);

        var outputSize = new FileInfo(outputPath).Length;
        return new CompilationResult(outputPath, files.InputFiles.Count, outputSize, _warnings);
    }

    private CollectedFiles CollectFiles(HhpProject project, bool flat, System.Text.Encoding helpTextEncoding)
    {
        var byArchivePath = new Dictionary<string, InputFile>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<InputFile>();
        var missingRequired = new List<string>();

        string? defaultTopicArchivePath = null;

        void AddPath(
            string rawPath,
            string reason,
            bool required,
            bool isProjectPath,
            string? baseDirectory = null,
            string? archiveBaseDirectory = null)
        {
            var cleaned = isProjectPath ? ArchivePath.CleanProjectPath(rawPath) : ArchivePath.CleanLink(rawPath);
            if (cleaned is null)
            {
                return;
            }

            var sourcePath = ResolveSourcePath(project.ProjectDirectory, cleaned, baseDirectory, isProjectPath);
            var archiveRelative = MakeArchiveRelative(project.ProjectDirectory, sourcePath, cleaned, flat, archiveBaseDirectory);
            if (archiveRelative.Length == 0)
            {
                return;
            }

            if (!File.Exists(sourcePath))
            {
                var message = $"{rawPath} ({reason})";
                if (required)
                {
                    missingRequired.Add(message);
                }

                _warnings.Add($"file not found: {message}");
                return;
            }

            if (byArchivePath.TryGetValue(archiveRelative, out var existing))
            {
                if (!Path.GetFullPath(existing.SourcePath).Equals(Path.GetFullPath(sourcePath), StringComparison.OrdinalIgnoreCase))
                {
                    _warnings.Add($"duplicate archive path '{archiveRelative}' from '{sourcePath}', keeping '{existing.SourcePath}'");
                }

                return;
            }

            var input = new InputFile(sourcePath, archiveRelative, reason, BuildInputData(sourcePath, helpTextEncoding, flat));
            byArchivePath.Add(archiveRelative, input);
            queue.Enqueue(input);

            if (_options.Verbose)
            {
                Console.Error.WriteLine($"add: {archiveRelative} <- {sourcePath}");
            }
        }

        foreach (var file in project.Files)
        {
            AddPath(file, "[FILES]", required: true, isProjectPath: true);
        }

        if (project.Option("Contents file") is { } contentsFile)
        {
            AddPath(contentsFile, "Contents file", required: true, isProjectPath: true);
        }

        if (project.Option("Index file") is { } indexFile)
        {
            AddPath(indexFile, "Index file", required: true, isProjectPath: true);
        }

        var defaultTopic = project.Option("Default topic") ?? project.Files.FirstOrDefault();
        if (defaultTopic is not null)
        {
            AddPath(defaultTopic, "Default topic", required: true, isProjectPath: true);
            var defaultSource = ResolveSourcePath(project.ProjectDirectory, ArchivePath.CleanProjectPath(defaultTopic) ?? defaultTopic, null, isProjectPath: true);
            defaultTopicArchivePath = MakeArchiveRelative(project.ProjectDirectory, defaultSource, defaultTopic, flat);
        }

        if (_options.ScanLinks)
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var archiveBaseDirectory = ArchivePath.DirectoryName(current.ArchivePath);
                foreach (var link in LinkScanner.ExtractLinks(current.SourcePath, helpTextEncoding))
                {
                    AddPath(
                        link,
                        $"linked from {current.ArchivePath}",
                        required: false,
                        isProjectPath: false,
                        baseDirectory: Path.GetDirectoryName(current.SourcePath),
                        archiveBaseDirectory: archiveBaseDirectory);
                }
            }
        }

        InputFile? generatedContents = null;
        if (project.Option("Contents file") is null)
        {
            generatedContents = BuildGeneratedContentsFile(byArchivePath.Values, helpTextEncoding);
            byArchivePath[generatedContents.ArchivePath] = generatedContents;
            _warnings.Add("No Contents file was specified; generated a simple table of contents.");
        }

        return new CollectedFiles(byArchivePath.Values.OrderBy(f => f.ArchivePath, ChmPathComparer.Instance).ToList(), missingRequired, defaultTopicArchivePath, generatedContents?.ArchivePath);
    }

    private string ResolveOutputPath(HhpProject project)
    {
        var configured = _options.OutputPath ?? project.Option("Compiled file");
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = Path.ChangeExtension(Path.GetFileName(project.ProjectPath), ".chm");
        }

        var outputPath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(project.ProjectDirectory, configured);

        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? project.ProjectDirectory);
        return outputPath;
    }

    private ChmMetadata BuildMetadata(HhpProject project, string outputPath, int lcid, System.Text.Encoding helpTextEncoding, string? defaultTopicArchivePath, bool contentsFileGenerated)
    {
        var compiledStem = Path.GetFileNameWithoutExtension(outputPath).ToLowerInvariant();
        var contentsFile = NormalizeOptionArchivePath(project, project.Option("Contents file"), project.OptionIsYes("Flat"))
            ?? (contentsFileGenerated ? "Table of Contents.hhc" : null);
        var defaultWindow = project.Option("Default Window") ?? "main";
        return new ChmMetadata(
            Title: project.Option("Title") ?? Path.GetFileNameWithoutExtension(project.ProjectPath),
            DefaultTopic: defaultTopicArchivePath,
            ContentsFile: contentsFile,
            IndexFile: NormalizeOptionArchivePath(project, project.Option("Index file"), project.OptionIsYes("Flat")),
            ContentsFileGenerated: contentsFileGenerated,
            DefaultWindow: defaultWindow,
            DefaultFont: project.Option("Default Font"),
            CompiledFileStem: compiledStem,
            Lcid: lcid,
            TextEncoding: helpTextEncoding,
            FullTextSearch: false);
    }

    private static string? NormalizeOptionArchivePath(HhpProject project, string? path, bool flat)
    {
        if (path is null)
        {
            return null;
        }

        var cleaned = ArchivePath.CleanProjectPath(path);
        if (cleaned is null)
        {
            return null;
        }

        var source = ResolveSourcePath(project.ProjectDirectory, cleaned, null, isProjectPath: true);
        return MakeArchiveRelative(project.ProjectDirectory, source, cleaned, flat);
    }

    private void WarnForUnsupportedOptions(HhpProject project)
    {
        if (project.OptionIsYes("Full-text search"))
        {
            _warnings.Add("Full-text search index generation is not implemented; the CHM will open without a search index.");
        }

        if (project.OptionIsYes("Binary TOC"))
        {
            _warnings.Add("Binary TOC is not implemented; the source .hhc file is embedded instead.");
        }

        if (project.OptionIsYes("Binary Index"))
        {
            _warnings.Add("Binary Index is not implemented; the source .hhk file is embedded instead.");
        }

        if (project.Sections.ContainsKey("MERGE FILES"))
        {
            _warnings.Add("[MERGE FILES] is preserved only as project metadata; merged CHM collections are not generated.");
        }

        if (project.Sections.ContainsKey("WINDOWS"))
        {
            _warnings.Add("[WINDOWS] custom settings are not parsed; a generated default window definition is used.");
        }
    }

    private static int? TryParseLcid(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var token = language.Trim().Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (token is null)
        {
            return null;
        }

        token = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;
        return int.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lcid) ? lcid : null;
    }

    private static string ResolveSourcePath(string projectDirectory, string path, string? baseDirectory, bool isProjectPath)
    {
        if (Path.IsPathRooted(path))
        {
            if (!isProjectPath && IsRootRelativePath(path))
            {
                var rooted = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.GetFullPath(Path.Combine(projectDirectory, rooted));
            }

            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory ?? projectDirectory, path));
    }

    private static bool IsRootRelativePath(string path)
    {
        if (IsUncPath(path))
        {
            return false;
        }

        return Path.GetPathRoot(path) is { } root && !root.Contains(':');
    }

    private static bool IsUncPath(string path)
    {
        return path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal);
    }

    private static string MakeArchiveRelative(string projectDirectory, string sourcePath, string originalPath, bool flat, string? archiveBaseDirectory = null)
    {
        string relative;
        if (IsUnderDirectory(projectDirectory, sourcePath))
        {
            relative = Path.GetRelativePath(projectDirectory, sourcePath);
        }
        else if (!string.IsNullOrEmpty(archiveBaseDirectory) && !IsRootedPath(originalPath))
        {
            relative = ArchivePath.Combine(archiveBaseDirectory, originalPath);
        }
        else
        {
            relative = IsRootedPath(originalPath) ? Path.GetFileName(sourcePath) : originalPath;
        }

        return ArchivePath.NormalizeForArchive(relative, flat);
    }

    private static bool IsRootedPath(string path)
    {
        return Path.IsPathRooted(path)
            || (path.Length >= 3
                && char.IsLetter(path[0])
                && path[1] == ':'
                && (path[2] == '\\' || path[2] == '/'));
    }

    private static bool IsUnderDirectory(string directory, string path)
    {
        var dir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(path);
        return full.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
    }

    private static InputFile BuildGeneratedContentsFile(IEnumerable<InputFile> files, System.Text.Encoding encoding)
    {
        var topics = files
            .Where(f => IsHtmlFile(f.ArchivePath))
            .OrderBy(f => f.ArchivePath, ChmPathComparer.Instance)
            .ToList();

        var lines = new List<string>
        {
            "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">",
            "<html>",
            "<head><meta name=\"GENERATOR\" content=\"KomuraHhc\"></head>",
            "<body>",
            "<ul>"
        };

        foreach (var topic in topics)
        {
            var title = Path.GetFileNameWithoutExtension(topic.ArchivePath);
            lines.Add("  <li><object type=\"text/sitemap\">");
            lines.Add($"    <param name=\"Name\" value=\"{EscapeHtml(title)}\">");
            lines.Add($"    <param name=\"Local\" value=\"{EscapeHtml(topic.ArchivePath)}\">");
            lines.Add("  </object></li>");
        }

        lines.Add("</ul>");
        lines.Add("</body>");
        lines.Add("</html>");

        var text = string.Join("\r\n", lines) + "\r\n";
        return new InputFile(string.Empty, "Table of Contents.hhc", "Generated contents", encoding.GetBytes(text));
    }

    private static byte[]? BuildInputData(string sourcePath, System.Text.Encoding helpTextEncoding, bool flat)
    {
        var extension = Path.GetExtension(sourcePath);
        if (extension.Equals(".hhc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hhk", StringComparison.OrdinalIgnoreCase))
        {
            var text = TextEncodingDetector.Read(sourcePath, helpTextEncoding).Text;
            if (flat)
            {
                text = LinkScanner.RewriteLinksForFlatArchive(text);
            }

            return helpTextEncoding.GetBytes(text);
        }

        if (!flat || !IsRewritableTextFile(extension))
        {
            return null;
        }

        var textFile = TextEncodingDetector.Read(sourcePath, helpTextEncoding);
        return textFile.Encoding.GetBytes(LinkScanner.RewriteLinksForFlatArchive(textFile.Text));
    }

    private static bool IsRewritableTextFile(string extension)
    {
        return extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".css", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtmlFile(string archivePath)
    {
        var extension = Path.GetExtension(archivePath);
        return extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private sealed record CollectedFiles(
        IReadOnlyList<InputFile> InputFiles,
        IReadOnlyList<string> MissingRequired,
        string? DefaultTopicArchivePath,
        string? GeneratedContentsFile);
}

internal sealed record InputFile(string SourcePath, string ArchivePath, string Reason, byte[]? Data = null);
