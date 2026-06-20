using System.Globalization;
using System.Text;

namespace Komura.Hhc;

internal static class TextEncodingDetector
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);

    static TextEncodingDetector()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static Encoding AnsiEncoding
    {
        get
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
    }

    public static TextFile Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var encoding = Detect(bytes);
        var text = encoding.GetString(bytes);
        return new TextFile(text.TrimStart('\uFEFF'), encoding);
    }

    private static Encoding Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
        {
            return Encoding.UTF8;
        }

        if (bytes.StartsWith(new byte[] { 0xFF, 0xFE }))
        {
            return Encoding.Unicode;
        }

        if (bytes.StartsWith(new byte[] { 0xFE, 0xFF }))
        {
            return Encoding.BigEndianUnicode;
        }

        try
        {
            _ = StrictUtf8.GetString(bytes);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return AnsiEncoding;
        }
    }
}

internal sealed record TextFile(string Text, Encoding Encoding);
