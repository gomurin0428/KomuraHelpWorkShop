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

        var baseHref = BaseHrefApplies(extension) ? ExtractBaseHref(text) : null;
        var scanText = baseHref is null ? text : BaseTagRegex().Replace(text, string.Empty);
        var links = new List<string>();
        foreach (Match match in AttributeLinkRegex().Matches(scanText))
        {
            AddGroup(links, match, baseHref);
        }

        ExtractLocalParamLinks(scanText, links, baseHref);

        foreach (Match match in CssImportStringRegex().Matches(scanText))
        {
            AddGroup(links, match, baseHref);
        }

        foreach (Match match in CssUrlRegex().Matches(scanText))
        {
            AddGroup(links, match, baseHref);
        }

        return links;
    }

    public static string RewriteLinksForFlatArchive(string text)
    {
        text = BaseTagRegex().Replace(text, RewriteBaseTagForFlatArchive);
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

    private static bool BaseHrefApplies(string extension)
    {
        return extension.Equals(".htm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hhc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".hhk", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddGroup(List<string> links, Match match, string? baseHref)
    {
        for (var i = 1; i < match.Groups.Count; i++)
        {
            if (match.Groups[i].Success && match.Groups[i].Value.Length > 0)
            {
                links.Add(ApplyBaseHref(match.Groups[i].Value, baseHref));
                return;
            }
        }
    }

    private static void ExtractLocalParamLinks(string text, List<string> links, string? baseHref)
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
                links.Add(ApplyBaseHref(value, baseHref));
            }
        }
    }

    private static string? ExtractBaseHref(string text)
    {
        foreach (Match tag in BaseTagRegex().Matches(text))
        {
            var href = GetAttributeValue(tag.Value, "href");
            if (!string.IsNullOrWhiteSpace(href))
            {
                return href;
            }
        }

        return null;
    }

    private static string RewriteBaseTagForFlatArchive(Match tag)
    {
        var href = GetAttributeValue(tag.Value, "href");
        if (string.IsNullOrWhiteSpace(href) || ArchivePath.CleanLink(href) is null)
        {
            return tag.Value;
        }

        return string.Empty;
    }

    private static string ApplyBaseHref(string value, string? baseHref)
    {
        if (string.IsNullOrWhiteSpace(baseHref) || !ShouldResolveAgainstBase(value))
        {
            return value;
        }

        var trimmedBase = baseHref.Trim();
        if (ArchivePath.CleanLink(trimmedBase) is { } localBase)
        {
            var baseDirectory = DirectoryPartForBaseHref(localBase, trimmedBase);
            return string.IsNullOrEmpty(baseDirectory)
                ? value
                : ArchivePath.Combine(baseDirectory, value);
        }

        if (Uri.TryCreate(trimmedBase, UriKind.Absolute, out var absoluteBase)
            && Uri.TryCreate(absoluteBase, value, out var resolved))
        {
            return resolved.ToString();
        }

        if (trimmedBase.StartsWith("//", StringComparison.Ordinal))
        {
            return trimmedBase.TrimEnd('/') + "/" + value;
        }

        return value;
    }

    private static bool ShouldResolveAgainstBase(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 0
            && !trimmed.StartsWith("#", StringComparison.Ordinal)
            && !trimmed.StartsWith("/", StringComparison.Ordinal)
            && !trimmed.StartsWith("\\", StringComparison.Ordinal)
            && !trimmed.StartsWith("//", StringComparison.Ordinal)
            && !HasScheme(trimmed);
    }

    private static bool HasScheme(string value)
    {
        var colon = value.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        for (var i = 0; i < colon; i++)
        {
            var ch = value[i];
            if (i == 0 ? !char.IsLetter(ch) : !(char.IsLetterOrDigit(ch) || ch == '+' || ch == '-' || ch == '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static string DirectoryPartForBaseHref(string cleanBaseHref, string rawBaseHref)
    {
        var normalized = cleanBaseHref.Replace('\\', '/').Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (rawBaseHref.EndsWith("/", StringComparison.Ordinal) || rawBaseHref.EndsWith("\\", StringComparison.Ordinal))
        {
            return normalized.TrimEnd('/');
        }

        var slash = normalized.LastIndexOf('/');
        return slash < 0 ? string.Empty : normalized[..slash];
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
        var cleaned = ArchivePath.CleanLink(value);
        if (cleaned is null)
        {
            return value;
        }

        var cut = FindSuffixStart(value);
        var rawTarget = cut >= 0 ? value[..cut] : value;
        var suffix = cut >= 0 ? value[cut..] : string.Empty;
        var target = cleaned.Replace('\\', '/');
        var slash = target.LastIndexOf('/');
        var fileName = slash >= 0 ? target[(slash + 1)..] : target;
        var rewrittenFileName = RawFileNameForFlattening(rawTarget, fileName);
        return rewrittenFileName.Length == 0 ? value : rewrittenFileName + suffix;
    }

    private static string RawFileNameForFlattening(string rawTarget, string decodedFileName)
    {
        var start = 0;
        for (var i = 0; i < rawTarget.Length; i++)
        {
            if (rawTarget[i] is '/' or '\\')
            {
                start = i + 1;
                continue;
            }

            var separatorLength = PercentEncodedSeparatorLength(rawTarget, i);
            if (separatorLength == 0)
            {
                separatorLength = CharacterReferenceSeparatorLength(rawTarget, i);
            }

            if (separatorLength > 0)
            {
                start = i + separatorLength;
                i += separatorLength - 1;
            }
        }

        var rawFileName = rawTarget[start..];
        return rawFileName.Length == 0 ? decodedFileName : rawFileName;
    }

    private static int PercentEncodedSeparatorLength(string value, int index)
    {
        if (index + 2 >= value.Length || value[index] != '%')
        {
            return 0;
        }

        var first = char.ToUpperInvariant(value[index + 1]);
        var second = char.ToUpperInvariant(value[index + 2]);
        return (first == '2' && second == 'F') || (first == '5' && second == 'C') ? 3 : 0;
    }

    private static int CharacterReferenceSeparatorLength(string value, int index)
    {
        if (value[index] != '&')
        {
            return 0;
        }

        var semicolon = value.IndexOf(';', index + 1);
        if (semicolon < 0)
        {
            return 0;
        }

        var body = value[(index + 1)..semicolon];
        return body.Equals("#47", StringComparison.OrdinalIgnoreCase)
            || body.Equals("#x2f", StringComparison.OrdinalIgnoreCase)
            || body.Equals("#92", StringComparison.OrdinalIgnoreCase)
            || body.Equals("#x5c", StringComparison.OrdinalIgnoreCase)
            || body.Equals("sol", StringComparison.OrdinalIgnoreCase)
            ? semicolon - index + 1
            : 0;
    }

    private static int FindSuffixStart(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '?')
            {
                return i;
            }

            if (value[i] == '#' && !IsNumericCharacterReferenceHash(value, i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsNumericCharacterReferenceHash(string value, int hashIndex)
    {
        if (hashIndex == 0 || value[hashIndex - 1] != '&' || hashIndex + 1 >= value.Length)
        {
            return false;
        }

        var first = value[hashIndex + 1];
        if (!char.IsDigit(first) && first is not 'x' and not 'X')
        {
            return false;
        }

        var semicolon = value.IndexOf(';', hashIndex + 1);
        return semicolon > hashIndex;
    }

    [GeneratedRegex("""(?is)\b(?:href|src)\s*=\s*(?:"([^"]+)"|'([^']+)'|([^\s>]+))""")]
    private static partial Regex AttributeLinkRegex();

    [GeneratedRegex("""(?is)<base\b[^>]*>""")]
    private static partial Regex BaseTagRegex();

    [GeneratedRegex("""(?is)<param\b[^>]*>""")]
    private static partial Regex ParamTagRegex();

    [GeneratedRegex("""(?is)\b([a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))""")]
    private static partial Regex HtmlAttributeRegex();

    [GeneratedRegex("""(?is)@import\s+(?:"([^"]+)"|'([^']+)')""")]
    private static partial Regex CssImportStringRegex();

    [GeneratedRegex("""(?is)\burl\s*\(\s*(?:"([^"]+)"|'([^']+)'|([^)'"\s]+))\s*\)""")]
    private static partial Regex CssUrlRegex();
}
