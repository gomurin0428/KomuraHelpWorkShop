using System.Text;
using System.Text.RegularExpressions;

namespace Komura.Hhc;

internal static partial class LinkScanner
{
    public static IReadOnlyList<string> ExtractLinks(string sourcePath, Encoding? fallbackEncoding = null)
    {
        var extension = Path.GetExtension(sourcePath);
        if (!IsScannable(extension))
        {
            return Array.Empty<string>();
        }

        string text;
        try
        {
            text = TextEncodingDetector.Read(sourcePath, fallbackEncoding).Text;
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

        ExtractLocalParamLinks(text, links);

        foreach (Match match in CssImportStringRegex().Matches(text))
        {
            AddGroup(links, match);
        }

        foreach (Match match in CssUrlRegex().Matches(text))
        {
            AddGroup(links, match);
        }

        return links;
    }

    public static string RewriteLinksForFlatArchive(string text)
    {
        text = AttributeLinkRegex().Replace(text, RewriteMatchValue);
        text = ParamTagRegex().Replace(text, RewriteLocalParamTag);
        text = CssImportStringRegex().Replace(text, RewriteMatchValue);
        text = CssUrlRegex().Replace(text, RewriteMatchValue);
        return text;
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

    private static void ExtractLocalParamLinks(string text, List<string> links)
    {
        foreach (Match tag in ParamTagRegex().Matches(text))
        {
            var name = GetAttributeValue(tag.Value, "name");
            if (!string.Equals(name, "Local", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = GetAttributeValue(tag.Value, "value");
            if (!string.IsNullOrEmpty(value))
            {
                links.Add(value);
            }
        }
    }

    private static string? GetAttributeValue(string tag, string attributeName)
    {
        foreach (Match attribute in HtmlAttributeRegex().Matches(tag))
        {
            if (!string.Equals(attribute.Groups[1].Value, attributeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            for (var i = 2; i < attribute.Groups.Count; i++)
            {
                if (attribute.Groups[i].Success)
                {
                    return attribute.Groups[i].Value;
                }
            }
        }

        return null;
    }

    private static string RewriteLocalParamTag(Match tag)
    {
        var name = GetAttributeValue(tag.Value, "name");
        if (!string.Equals(name, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return tag.Value;
        }

        return HtmlAttributeRegex().Replace(tag.Value, attribute =>
        {
            if (!string.Equals(attribute.Groups[1].Value, "value", StringComparison.OrdinalIgnoreCase))
            {
                return attribute.Value;
            }

            return RewriteMatchValue(attribute, firstValueGroup: 2);
        });
    }

    private static string RewriteMatchValue(Match match)
    {
        return RewriteMatchValue(match, firstValueGroup: 1);
    }

    private static string RewriteMatchValue(Match match, int firstValueGroup)
    {
        for (var i = firstValueGroup; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            if (!group.Success)
            {
                continue;
            }

            var rewritten = FlattenLocalTarget(group.Value);
            if (rewritten == group.Value)
            {
                return match.Value;
            }

            var relativeStart = group.Index - match.Index;
            return match.Value[..relativeStart] + rewritten + match.Value[(relativeStart + group.Length)..];
        }

        return match.Value;
    }

    private static string FlattenLocalTarget(string value)
    {
        if (ArchivePath.CleanLink(value) is null)
        {
            return value;
        }

        var cut = value.IndexOfAny(new[] { '#', '?' });
        var target = cut >= 0 ? value[..cut] : value;
        var suffix = cut >= 0 ? value[cut..] : string.Empty;
        var slash = target.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = slash >= 0 ? target[(slash + 1)..] : target;
        return fileName.Length == 0 ? value : fileName + suffix;
    }

    [GeneratedRegex("""(?is)\b(?:href|src)\s*=\s*(?:"([^"]+)"|'([^']+)'|([^\s>]+))""")]
    private static partial Regex AttributeLinkRegex();

    [GeneratedRegex("""(?is)<param\b[^>]*>""")]
    private static partial Regex ParamTagRegex();

    [GeneratedRegex("""(?is)\b([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))""")]
    private static partial Regex HtmlAttributeRegex();

    [GeneratedRegex("""(?is)@import\s+(?:"([^"]+)"|'([^']+)')""")]
    private static partial Regex CssImportStringRegex();

    [GeneratedRegex("""(?is)\burl\s*\(\s*(?:"([^"]+)"|'([^']+)'|([^)'"\s]+))\s*\)""")]
    private static partial Regex CssUrlRegex();
}
