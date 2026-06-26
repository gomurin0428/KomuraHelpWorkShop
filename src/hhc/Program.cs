using System.Diagnostics;

namespace Komura.Hhc;

internal static class Program
{
    public static int Main(string[] args)
    {
        var options = CliOptions.Parse(args, Console.Error);
        if (options is null)
        {
            CliOptions.PrintHelp(Console.Out);
            return 24;
        }

        if (options.ShowHelp || options.ShowVersion)
        {
            CliOptions.PrintHelp(Console.Out);
            return 24;
        }

        Debug.Assert(options.ProjectPath is not null);

        try
        {
            var compiler = new ProjectCompiler(options);
            var result = compiler.Compile();

            Console.WriteLine($"{VersionInfo.Banner}");
            foreach (var warning in result.Warnings)
            {
                Console.Error.WriteLine(warning);
            }

            Console.WriteLine($"Compiled: {result.OutputPath}");
            Console.WriteLine($"Files: {result.FileCount}, size: {result.OutputSize:N0} bytes");
            return result.HadMissingRequiredFiles ? 0 : 1;
        }
        catch (CompilationException ex) when (ex.Message.StartsWith("Project file not found: ", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Unable to open {ex.Message[24..]}.");
            return 0;
        }
        catch (CompilationException ex)
        {
            foreach (var warning in ex.Warnings)
            {
                Console.Error.WriteLine(warning);
            }

            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }
}
