namespace Komura.Hhc;

internal sealed class ChmPathComparer : IComparer<string>
{
    public static readonly ChmPathComparer Instance = new();

    private ChmPathComparer()
    {
    }

    public int Compare(string? x, string? y)
    {
        return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
