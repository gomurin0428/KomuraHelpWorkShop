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
        var files = CollectFiles(project, flat);
        if (files.MissingRequired.Count > 0 && !_options.AllowMissing)
        {
            throw new CompilationException(
                "Missing required files: " + string.Join(", ", files.MissingRequired.Take(8))
                + (files.MissingRequired.Count > 8 ? " ..." : string.Empty));
        }

        WarnForUnsupportedOptions(project);

        var outputPath = ResolveOutputPath(project);
        var metadata = BuildMetadata(project, outputPath, files.DefaultTopicArchivePath);
        var writer = new ChmWriter();
        writer.Write(outputPath, files.InputFiles, metadata);

        var outputSize = new FileInfo(outputPath).Length;
        return new CompilationResult(outputPath, files.InputFiles.Count, outputSize, _warnings);
    }

    private CollectedFiles CollectFiles(HhpProject project, bool flat)
    {
        var byArchivePath = new Dictionary<string, InputFile>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<InputFile>();
        var missingRequired = new List<string>();

        string? defaultTopicArchivePath = null;

        void AddPath(string rawPath, string reason, bool required, string? baseDirectory = null)
        {
            var cleaned = ArchivePath.CleanLink(rawPath);
            if (cleaned is null)
            {
                return;
            }

            var sourcePath = ResolveSourcePath(project.ProjectDirectory, cleaned, baseDirectory);
            var archiveRelative = MakeArchiveRelative(project.ProjectDirectory, sourcePath, cleaned, flat);
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

            var input = new InputFile(sourcePath, archiveRelative, reason);
            byArchivePath.Add(archiveRelative, input);
            queue.Enqueue(input);

            if (_options.Verbose)
            {
                Console.Error.WriteLine($"add: {archiveRelative} <- {sourcePath}");
            }
        }

        foreach (var file in project.Files)
        {
            AddPath(file, "[FILES]", required: true);
        }

        if (project.Option("Contents file") is { } contentsFile)
        {
            AddPath(contentsFile, "Contents file", required: true);
        }

        if (project.Option("Index file") is { } indexFile)
        {
            AddPath(indexFile, "Index file", required: true);
        }

        var defaultTopic = project.Option("Default topic") ?? project.Files.FirstOrDefault();
        if (defaultTopic is not null)
        {
            AddPath(defaultTopic, "Default topic", required: true);
            var defaultSource = ResolveSourcePath(project.ProjectDirectory, ArchivePath.CleanLink(defaultTopic) ?? defaultTopic, null);
            defaultTopicArchivePath = MakeArchiveRelative(project.ProjectDirectory, defaultSource, defaultTopic, flat);
        }

        if (_options.ScanLinks)
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var link in LinkScanner.ExtractLinks(current.SourcePath))
                {
                    AddPath(link, $"linked from {current.ArchivePath}", required: false, Path.GetDirectoryName(current.SourcePath));
                }
            }
        }

        return new CollectedFiles(byArchivePath.Values.OrderBy(f => f.ArchivePath, ChmPathComparer.Instance).ToList(), missingRequired, defaultTopicArchivePath);
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

    private ChmMetadata BuildMetadata(HhpProject project, string outputPath, string? defaultTopicArchivePath)
    {
        var lcid = TryParseLcid(project.Option("Language")) ?? CultureInfo.CurrentCulture.LCID;
        var compiledStem = Path.GetFileNameWithoutExtension(outputPath).ToLowerInvariant();
        return new ChmMetadata(
            Title: project.Option("Title") ?? Path.GetFileNameWithoutExtension(project.ProjectPath),
            DefaultTopic: defaultTopicArchivePath,
            ContentsFile: NormalizeOptionArchivePath(project, project.Option("Contents file"), project.OptionIsYes("Flat")),
            IndexFile: NormalizeOptionArchivePath(project, project.Option("Index file"), project.OptionIsYes("Flat")),
            DefaultWindow: project.Option("Default Window"),
            DefaultFont: project.Option("Default Font"),
            CompiledFileStem: compiledStem,
            Lcid: lcid,
            TextEncoding: project.TextEncoding,
            FullTextSearch: false);
    }

    private static string? NormalizeOptionArchivePath(HhpProject project, string? path, bool flat)
    {
        if (path is null)
        {
            return null;
        }

        var cleaned = ArchivePath.CleanLink(path);
        if (cleaned is null)
        {
            return null;
        }

        var source = ResolveSourcePath(project.ProjectDirectory, cleaned, null);
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

    private static string ResolveSourcePath(string projectDirectory, string path, string? baseDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            var rooted = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetPathRoot(path);
            if (root is not null && !root.Contains(':'))
            {
                return Path.GetFullPath(Path.Combine(projectDirectory, rooted));
            }

            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory ?? projectDirectory, path));
    }

    private static string MakeArchiveRelative(string projectDirectory, string sourcePath, string originalPath, bool flat)
    {
        string relative;
        if (IsUnderDirectory(projectDirectory, sourcePath))
        {
            relative = Path.GetRelativePath(projectDirectory, sourcePath);
        }
        else
        {
            relative = Path.IsPathRooted(originalPath) ? Path.GetFileName(sourcePath) : originalPath;
        }

        return ArchivePath.NormalizeForArchive(relative, flat);
    }

    private static bool IsUnderDirectory(string directory, string path)
    {
        var dir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(path);
        return full.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CollectedFiles(
        IReadOnlyList<InputFile> InputFiles,
        IReadOnlyList<string> MissingRequired,
        string? DefaultTopicArchivePath);
}

internal sealed record InputFile(string SourcePath, string ArchivePath, string Reason);
