namespace Komura.Hhc;

internal sealed record CompilationResult(
    string OutputPath,
    int FileCount,
    long OutputSize,
    IReadOnlyList<string> Warnings);
