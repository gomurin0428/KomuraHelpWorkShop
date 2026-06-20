namespace Komura.Hhc;

internal sealed class CompilationException : Exception
{
    public CompilationException(string message)
        : base(message)
    {
    }
}
