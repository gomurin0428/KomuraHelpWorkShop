using System.Net;

namespace Komura.Hhc;

internal static class ArchivePath
{
    public static string? CleanLink(string raw)
    {
        return Clean(raw, stripFragmentAndQuery: true, decodeHtmlEntities: true, decodePercentEscapes: true, rejectExternalReferences: true);
    }

    public static string? CleanProjectPath(string raw)
    {
        return Clean(raw, stripFragmentAndQuery: false, decodeHtmlEntities: false, decodePercentEscapes: false, rejectExternalReferences: false);
    }

    private static string? Clean(
        string raw,
        bool stripFragmentAndQuery,
        bool decodeHtmlEntities,
        bool decodePercentEscapes,
        bool rejectExternalReferences)
    {
        var value = (decodeHtmlEntities ? WebUtility.HtmlDecode(raw) : raw).Trim();
        if (value.Length == 0)
        {
            return null;
        }

        if (stripFragmentAndQuery && value.StartsWith('#'))
        {
            return null;
        }

        if (rejectExternalReferences && IsExternalReference(value))
        {
            return null;
        }

        if (stripFragmentAndQuery)
        {
            var cut = value.IndexOfAny(new[] { '#', '?' });
            if (cut >= 0)
            {
                value = value[..cut];
            }
        }

        value = value.Trim();
        if (value.Length == 0)
        {
            return null;
        }

        if (decodePercentEscapes)
        {
            try
            {
                value = Uri.UnescapeDataString(value);
            }
            catch
            {
                // Keep the original spelling when percent decoding is malformed.
            }
        }

        if (value.Contains('\0'))
        {
            return null;
        }

        return value
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool IsExternalReference(string value)
    {
        return value.StartsWith("//", StringComparison.Ordinal)
            || value.StartsWith(@"\\", StringComparison.Ordinal)
            || value.StartsWith("mk:@MSITStore:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("ms-its:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("its:", StringComparison.OrdinalIgnoreCase)
            || HasAbsoluteUriScheme(value);
    }

    private static bool HasAbsoluteUriScheme(string value)
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

        return !(colon == 1
            && colon + 1 < value.Length
            && (value[colon + 1] == '\\' || value[colon + 1] == '/'));
    }

    public static string NormalizeForArchive(string path, bool flat)
    {
        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        normalized = normalized.TrimStart('/');
        if (flat)
        {
            var lastSlash = normalized.LastIndexOf('/');
            normalized = lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
        }

        var parts = new List<string>();
        foreach (var part in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".")
            {
                continue;
            }

            if (part == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }

                continue;
            }

            parts.Add(part);
        }

        return string.Join('/', parts);
    }

    public static string ForDirectory(string archivePath)
    {
        return "/" + NormalizeForArchive(archivePath, flat: false);
    }

    public static string? DirectoryName(string archivePath)
    {
        var normalized = NormalizeForArchive(archivePath, flat: false);
        var slash = normalized.LastIndexOf('/');
        return slash <= 0 ? null : normalized[..slash];
    }

    public static string Combine(string? archiveDirectory, string path)
    {
        if (string.IsNullOrEmpty(archiveDirectory))
        {
            return path;
        }

        return archiveDirectory + "/" + path.Replace('\\', '/');
    }
}
