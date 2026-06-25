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
        var lcid = TryParseSupportedLcid(project.Option("Language")) ?? CultureInfo.CurrentCulture.LCID;
        var helpTextEncoding = TextEncodingDetector.ForLcid(lcid);
        var files = CollectFiles(project, flat, helpTextEncoding);

        WarnForUnsupportedOptions(project);

        var outputPath = ResolveOutputPath(project);
        ValidateOutputDoesNotOverwriteInputs(outputPath, project.ProjectPath, files.SourcePathsForOverwriteGuard);
        var metadata = BuildMetadata(project, outputPath, lcid, helpTextEncoding, files.DefaultTopicArchivePath);
        var writer = new ChmWriter();
        writer.Write(outputPath, files.InputFiles, metadata);

        var outputSize = new FileInfo(outputPath).Length;
        return new CompilationResult(outputPath, files.InputFiles.Count, outputSize, files.MissingRequired.Count > 0, _warnings);
    }

    private CollectedFiles CollectFiles(HhpProject project, bool flat, System.Text.Encoding helpTextEncoding)
    {
        var byArchivePath = new Dictionary<string, InputFile>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<InputFile>();
        var missingRequired = new List<string>();
        var rootArchivePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var linksByArchivePath = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var sourcePathsForOverwriteGuard = new HashSet<string>(FileSystemPathComparer);

        string? defaultTopicArchivePath = null;

        string? AddPath(
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
                return null;
            }

            var sourcePath = ResolveSourcePath(project.ProjectDirectory, cleaned, baseDirectory, isProjectPath);
            var archiveRelative = MakeArchiveRelative(project.ProjectDirectory, sourcePath, cleaned, flat, isProjectPath, archiveBaseDirectory);
            if (archiveRelative.Length == 0)
            {
                return null;
            }

            if (!File.Exists(sourcePath))
            {
                if (required)
                {
                    missingRequired.Add(rawPath);
                    _warnings.Add($"HHC5003: Error: Compilation failed while compiling {rawPath}.");
                }

                return null;
            }

            var fullSourcePath = Path.GetFullPath(sourcePath);
            if (required)
            {
                rootArchivePaths.Add(archiveRelative);
                sourcePathsForOverwriteGuard.Add(fullSourcePath);
            }

            var input = new InputFile(sourcePath, archiveRelative, reason);
            if (byArchivePath.TryGetValue(archiveRelative, out var existing))
            {
                if (Path.GetFullPath(existing.SourcePath).Equals(Path.GetFullPath(sourcePath), FileSystemPathComparison))
                {
                    return archiveRelative;
                }

                byArchivePath[archiveRelative] = input;
            }
            else
            {
                byArchivePath.Add(archiveRelative, input);
            }

            queue.Enqueue(input);

            if (_options.Verbose)
            {
                Console.Error.WriteLine($"add: {archiveRelative} <- {sourcePath}");
            }

            return archiveRelative;
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

        var configuredDefaultTopic = project.Option("Default topic");
        var defaultTopic = configuredDefaultTopic ?? project.Files.FirstOrDefault(IsHtmlPath);
        if (defaultTopic is not null)
        {
            if (configuredDefaultTopic is not null)
            {
                AddPath(defaultTopic, "Default topic", required: true, isProjectPath: true);
            }

            var defaultSource = ResolveSourcePath(project.ProjectDirectory, ArchivePath.CleanProjectPath(defaultTopic) ?? defaultTopic, null, isProjectPath: true);
            defaultTopicArchivePath = MakeArchiveRelative(project.ProjectDirectory, defaultSource, defaultTopic, flat, isProjectPath: true);
        }

        if (_options.ScanLinks)
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!byArchivePath.TryGetValue(current.ArchivePath, out var active) || !ReferenceEquals(active, current))
                {
                    continue;
                }

                var archiveBaseDirectory = ArchivePath.DirectoryName(current.ArchivePath);
                var scannedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                linksByArchivePath[current.ArchivePath] = scannedLinks;
                foreach (var link in LinkScanner.ExtractLinks(current.SourcePath, helpTextEncoding))
                {
                    var linkedArchivePath = AddPath(
                        link,
                        $"linked from {current.ArchivePath}",
                        required: false,
                        isProjectPath: false,
                        baseDirectory: Path.GetDirectoryName(current.SourcePath),
                        archiveBaseDirectory: archiveBaseDirectory);
                    if (linkedArchivePath is not null)
                    {
                        scannedLinks.Add(linkedArchivePath);
                    }
                }
            }
        }

        var reachableArchivePaths = BuildReachableArchivePaths(rootArchivePaths, byArchivePath, linksByArchivePath);
        var inputFiles = byArchivePath
            .Values
            .Where(file => reachableArchivePaths.Contains(file.ArchivePath))
            .OrderBy(f => f.ArchivePath, ChmPathComparer.Instance)
            .ToList();
        foreach (var inputFile in inputFiles)
        {
            sourcePathsForOverwriteGuard.Add(Path.GetFullPath(inputFile.SourcePath));
        }

        return new CollectedFiles(
            inputFiles,
            missingRequired,
            defaultTopicArchivePath,
            sourcePathsForOverwriteGuard.OrderBy(path => path, FileSystemPathComparer).ToList());
    }

    private string ResolveOutputPath(HhpProject project)
    {
        var configured = NormalizeFileSystemPath(_options.OutputPath ?? project.Option("Compiled file"));
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

    private static void ValidateOutputDoesNotOverwriteInputs(string outputPath, string projectPath, IReadOnlyList<string> inputSourcePaths)
    {
        if (SameFileSystemPath(outputPath, projectPath))
        {
            throw new CompilationException($"Output path would overwrite the project file: {projectPath}");
        }

        foreach (var inputSourcePath in inputSourcePaths)
        {
            if (inputSourcePath.Length == 0)
            {
                continue;
            }

            if (SameFileSystemPath(outputPath, inputSourcePath))
            {
                throw new CompilationException($"Output path would overwrite an input file: {inputSourcePath}");
            }
        }
    }

    private static bool SameFileSystemPath(string left, string right)
    {
        return Path.GetFullPath(left).Equals(Path.GetFullPath(right), FileSystemPathComparison);
    }

    private static StringComparison FileSystemPathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static StringComparer FileSystemPathComparer =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static HashSet<string> BuildReachableArchivePaths(
        HashSet<string> rootArchivePaths,
        Dictionary<string, InputFile> byArchivePath,
        Dictionary<string, HashSet<string>> linksByArchivePath)
    {
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>();
        foreach (var rootArchivePath in rootArchivePaths)
        {
            if (byArchivePath.ContainsKey(rootArchivePath))
            {
                pending.Push(rootArchivePath);
            }
        }

        while (pending.Count > 0)
        {
            var archivePath = pending.Pop();
            if (!reachable.Add(archivePath))
            {
                continue;
            }

            if (!linksByArchivePath.TryGetValue(archivePath, out var linkedArchivePaths))
            {
                continue;
            }

            foreach (var linkedArchivePath in linkedArchivePaths)
            {
                if (byArchivePath.ContainsKey(linkedArchivePath))
                {
                    pending.Push(linkedArchivePath);
                }
            }
        }

        return reachable;
    }


    private static string? NormalizeFileSystemPath(string? path)
    {
        return path?
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private ChmMetadata BuildMetadata(HhpProject project, string outputPath, int lcid, System.Text.Encoding helpTextEncoding, string? defaultTopicArchivePath)
    {
        var compiledStem = Path.GetFileNameWithoutExtension(outputPath).ToLowerInvariant();
        var contentsFile = NormalizeOptionArchivePath(project, project.Option("Contents file"), project.OptionIsYes("Flat"));
        var defaultWindow = project.Option("Default Window") ?? "main";
        return new ChmMetadata(
            Title: project.Option("Title") ?? Path.GetFileNameWithoutExtension(project.ProjectPath),
            DefaultTopic: defaultTopicArchivePath,
            ContentsFile: contentsFile,
            IndexFile: NormalizeOptionArchivePath(project, project.Option("Index file"), project.OptionIsYes("Flat")),
            ContentsFileGenerated: false,
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
        return MakeArchiveRelative(project.ProjectDirectory, source, cleaned, flat, isProjectPath: true);
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

    private static int? TryParseSupportedLcid(string? language)
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
        if (!int.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lcid))
        {
            return null;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(lcid);
            return lcid;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
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

    private static string MakeArchiveRelative(string projectDirectory, string sourcePath, string originalPath, bool flat, bool isProjectPath, string? archiveBaseDirectory = null)
    {
        string relative;
        if (IsUnderDirectory(projectDirectory, sourcePath))
        {
            relative = Path.GetRelativePath(projectDirectory, sourcePath);
        }
        else if (isProjectPath)
        {
            if (ArchivePath.NormalizeForArchive(originalPath, flat: false).Length == 0)
            {
                return string.Empty;
            }

            relative = Path.GetFileName(sourcePath);
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

    private static bool IsHtmlFile(string archivePath)
    {
        var extension = Path.GetExtension(archivePath);
        return extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtmlPath(string path)
    {
        var cleaned = ArchivePath.CleanProjectPath(path) ?? path;
        return IsHtmlFile(cleaned);
    }

    private sealed record CollectedFiles(
        IReadOnlyList<InputFile> InputFiles,
        IReadOnlyList<string> MissingRequired,
        string? DefaultTopicArchivePath,
        IReadOnlyList<string> SourcePathsForOverwriteGuard);
}

internal sealed record InputFile(string SourcePath, string ArchivePath, string Reason, byte[]? Data = null);
