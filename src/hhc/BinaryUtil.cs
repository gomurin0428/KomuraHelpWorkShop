using System.Buffers.Binary;

namespace Komura.Hhc;

internal static class BinaryUtil
{
    public static void WriteAscii(this BinaryWriter writer, string value)
    {
        foreach (var ch in value)
        {
            writer.Write((byte)ch);
        }
    }

    public static void WriteGuid(this BinaryWriter writer, string value)
    {
        writer.Write(new Guid(value).ToByteArray());
    }

    public static void WriteUInt16LittleEndian(Span<byte> buffer, int offset, int value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], checked((ushort)value));
    }

    public static void WriteInt32LittleEndian(Span<byte> buffer, int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], value);
    }

    public static void WriteUInt32LittleEndian(Span<byte> buffer, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[offset..], value);
    }
}
