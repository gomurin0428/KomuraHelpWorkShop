using System.Globalization;
using System.Text;

namespace Komura.Hhc;

internal sealed class HhpProject
{
    private HhpProject(
        string projectPath,
        string projectDirectory,
        Encoding textEncoding,
        Dictionary<string, string> options,
        Dictionary<string, List<string>> sections)
    {
        ProjectPath = projectPath;
        ProjectDirectory = projectDirectory;
        TextEncoding = textEncoding;
        Options = options;
        Sections = sections;
    }

    public string ProjectPath { get; }
    public string ProjectDirectory { get; }
    public Encoding TextEncoding { get; }
    public IReadOnlyDictionary<string, string> Options { get; }
    public IReadOnlyDictionary<string, List<string>> Sections { get; }

    public IReadOnlyList<string> Files =>
        Sections.TryGetValue("FILES", out var files) ? files : Array.Empty<string>();

    public static HhpProject Load(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new CompilationException($"Project file not found: {fullPath}");
        }

        var declaredEncoding = DetectDeclaredLanguageEncoding(fullPath);
        var textFile = TextEncodingDetector.Read(fullPath, declaredEncoding);
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? section = null;

        foreach (var rawLine in SplitLines(textFile.Text))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
            {
                section = line[1..^1].Trim();
                if (!sections.ContainsKey(section))
                {
                    sections[section] = new List<string>();
                }

                continue;
            }

            if (section is null)
            {
                continue;
            }

            if (section.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var equals = line.IndexOf('=');
                if (equals > 0)
                {
                    var key = line[..equals].Trim();
                    var value = line[(equals + 1)..].Trim();
                    options[key] = Unquote(value);
                }
            }
            else
            {
                sections[section].Add(Unquote(line));
            }
        }

        return new HhpProject(
            fullPath,
            Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory(),
            textFile.Encoding,
            options,
            sections);
    }

    public string? Option(string name)
    {
        return Options.TryGetValue(name, out var value) && value.Length > 0 ? value : null;
    }

    public bool OptionIsYes(string name)
    {
        var value = Option(name);
        return value is not null
            && (value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static Encoding? DetectDeclaredLanguageEncoding(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var asciiText = Encoding.Latin1.GetString(bytes);
        string? section = null;

        foreach (var rawLine in SplitLines(asciiText))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
            {
                section = line[1..^1].Trim();
                continue;
            }

            if (!string.Equals(section, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var equals = line.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            var key = line[..equals].Trim();
            if (!key.Equals("Language", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = Unquote(line[(equals + 1)..].Trim());
            if (TryParseLcid(value) is { } lcid)
            {
                return TextEncodingDetector.ForLcid(lcid);
            }
        }

        return null;
    }

    private static int? TryParseLcid(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var token = language.Trim().Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (token is null)
        {
            return null;
        }

        token = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;
        return int.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lcid) ? lcid : null;
    }
}
