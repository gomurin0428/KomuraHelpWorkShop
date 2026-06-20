using System.Text.RegularExpressions;

namespace Komura.Hhc;

internal static partial class LinkScanner
{
    public static IReadOnlyList<string> ExtractLinks(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        if (!IsScannable(extension))
        {
            return Array.Empty<string>();
        }

        string text;
        try
        {
            text = TextEncodingDetector.Read(sourcePath).Text;
        }
        catch
        {
            return Array.Empty<string>();
        }

        var links = new List<string>();
        foreach (Match match in AttributeLinkRegex().Matches(text))
        {
            AddGroup(links, match);
        }

        foreach (Match match in ParamLocalRegex().Matches(text))
        {
            AddGroup(links, match);
        }

        foreach (Match match in CssUrlRegex().Matches(text))
        {
            AddGroup(links, match);
        }

        return links;
    }

    private static bool IsScannable(string extension)
    {
        return extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hhc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hhk", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".css", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddGroup(List<string> links, Match match)
    {
        for (var i = 1; i < match.Groups.Count; i++)
        {
            if (match.Groups[i].Success && match.Groups[i].Value.Length > 0)
            {
                links.Add(match.Groups[i].Value);
                return;
            }
        }
    }

    [GeneratedRegex("""(?is)\b(?:href|src)\s*=\s*(?:"([^"]+)"|'([^']+)'|([^\s>]+))""")]
    private static partial Regex AttributeLinkRegex();

    [GeneratedRegex("""(?is)<param\b(?=[^>]*\bname\s*=\s*(?:"Local"|'Local'|Local))[^>]*\bvalue\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))""")]
    private static partial Regex ParamLocalRegex();

    [GeneratedRegex("""(?is)\burl\s*\(\s*(?:"([^"]+)"|'([^']+)'|([^)'"\s]+))\s*\)""")]
    private static partial Regex CssUrlRegex();
}
