using System.Net;

namespace Komura.Hhc;

internal static class ArchivePath
{
    private static readonly string[] ExternalSchemes =
    {
        "http:", "https:", "ftp:", "mailto:", "javascript:", "data:", "about:", "news:", "tel:"
    };

    public static string? CleanLink(string raw)
    {
        return Clean(raw, stripFragmentAndQuery: true);
    }

    public static string? CleanProjectPath(string raw)
    {
        return Clean(raw, stripFragmentAndQuery: false);
    }

    private static string? Clean(string raw, bool stripFragmentAndQuery)
    {
        var value = WebUtility.HtmlDecode(raw).Trim();
        if (value.Length == 0 || value.StartsWith('#'))
        {
            return null;
        }

        if (stripFragmentAndQuery
            && (value.StartsWith("//", StringComparison.Ordinal)
                || value.StartsWith(@"\\", StringComparison.Ordinal)))
        {
            return null;
        }

        foreach (var scheme in ExternalSchemes)
        {
            if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        if (value.StartsWith("mk:@MSITStore:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("ms-its:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("its:", StringComparison.OrdinalIgnoreCase))
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

        try
        {
            value = Uri.UnescapeDataString(value);
        }
        catch
        {
            // Keep the original spelling when percent decoding is malformed.
        }

        return value
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
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
