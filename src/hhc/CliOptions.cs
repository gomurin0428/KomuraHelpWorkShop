namespace Komura.Hhc;

internal sealed class CliOptions
{
    public string? ProjectPath { get; private init; }
    public string? OutputPath { get; private init; }
    public bool AllowMissing { get; private init; }
    public bool ScanLinks { get; private init; } = true;
    public bool Verbose { get; private init; }
    public bool ShowHelp { get; private init; }
    public bool ShowVersion { get; private init; }

    public static CliOptions? Parse(IReadOnlyList<string> args, TextWriter error)
    {
        if (args.Count == 0)
        {
            return new CliOptions { ShowHelp = true };
        }

        string? projectPath = null;
        string? outputPath = null;
        var allowMissing = false;
        var scanLinks = true;
        var verbose = false;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                case "/?":
                    return new CliOptions { ShowHelp = true };

                case "--version":
                    return new CliOptions { ShowVersion = true };

                case "-o":
                case "--out":
                    if (i + 1 >= args.Count)
                    {
                        return null;
                    }

                    outputPath = args[++i];
                    break;

                case "--allow-missing":
                    allowMissing = true;
                    break;

                case "--no-link-scan":
                    scanLinks = false;
                    break;

                case "-v":
                case "--verbose":
                    verbose = true;
                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    if (projectPath is not null)
                    {
                        return null;
                    }

                    projectPath = arg;
                    break;
            }
        }

        if (projectPath is null)
        {
            return null;
        }

        return new CliOptions
        {
            ProjectPath = projectPath,
            OutputPath = outputPath,
            AllowMissing = allowMissing,
            ScanLinks = scanLinks,
            Verbose = verbose
        };
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine(VersionInfo.Banner);
        output.WriteLine();
        output.WriteLine("Usage:");
        output.WriteLine("  hhc <project.hhp> [--out <file.chm>] [options]");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  -o, --out <path>     Override [OPTIONS] Compiled file.");
        output.WriteLine("  --no-link-scan       Only include files explicitly listed in the project.");
        output.WriteLine("  --allow-missing      Emit a CHM even when listed files are missing.");
        output.WriteLine("  -v, --verbose        Print file collection details.");
        output.WriteLine("  --version            Print version information.");
        output.WriteLine("  -h, --help           Show this help.");
    }
}
