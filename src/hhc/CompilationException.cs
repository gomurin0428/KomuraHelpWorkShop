namespace Komura.Hhc;

internal sealed class CompilationException : Exception
{
    public CompilationException(string message)
        : base(message)
    {
        Warnings = Array.Empty<string>();
    }

    public CompilationException(string message, IReadOnlyList<string> warnings)
        : base(message)
    {
        Warnings = warnings;
    }

    public IReadOnlyList<string> Warnings { get; }
}
