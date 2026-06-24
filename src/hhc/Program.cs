using System.Diagnostics;

namespace Komura.Hhc;

internal static class Program
{
    public static int Main(string[] args)
    {
        var options = CliOptions.Parse(args, Console.Error);
        if (options is null)
        {
            return 2;
        }

        if (options.ShowHelp)
        {
            CliOptions.PrintHelp(Console.Out);
            return 0;
        }

        if (options.ShowVersion)
        {
            Console.WriteLine(VersionInfo.Banner);
            return 0;
        }

        Debug.Assert(options.ProjectPath is not null);

        try
        {
            var compiler = new ProjectCompiler(options);
            var result = compiler.Compile();

            Console.WriteLine($"{VersionInfo.Banner}");
            foreach (var warning in result.Warnings)
            {
                Console.Error.WriteLine($"warning: {warning}");
            }

            Console.WriteLine($"Compiled: {result.OutputPath}");
            Console.WriteLine($"Files: {result.FileCount}, size: {result.OutputSize:N0} bytes");
            return 0;
        }
        catch (CompilationException ex)
        {
            foreach (var warning in ex.Warnings)
            {
                Console.Error.WriteLine($"warning: {warning}");
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
