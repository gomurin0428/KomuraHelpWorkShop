using System.Text;

namespace Komura.Hhc;

internal sealed record ChmMetadata(
    string Title,
    string? DefaultTopic,
    string? ContentsFile,
    string? IndexFile,
    string? DefaultWindow,
    string? DefaultFont,
    string CompiledFileStem,
    int Lcid,
    Encoding TextEncoding,
    bool FullTextSearch);
