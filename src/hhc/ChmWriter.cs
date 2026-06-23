using System.Text;

namespace Komura.Hhc;

internal sealed class ChmWriter
{
    private const int HeaderLength = 0x60;
    private const int HeaderSection0Length = 0x18;
    private const int DirectoryHeaderLength = 0x54;
    private const int DirectoryBlockLength = 0x1000;
    private const int QuickRefDensity = 2;
    private const int QuickRefInterval = 1 + (1 << QuickRefDensity);

    private readonly Action<string, Action<Stream>> _writeTempFile;

    public ChmWriter()
        : this(WriteTempFile)
    {
    }

    internal ChmWriter(Action<string, Action<Stream>> writeTempFile)
    {
        _writeTempFile = writeTempFile ?? throw new ArgumentNullException(nameof(writeTempFile));
    }

    public void Write(string outputPath, IReadOnlyList<InputFile> inputFiles, ChmMetadata metadata)
    {
        var entries = BuildEntries(inputFiles, metadata);
        AssignContentOffsets(entries);

        var directory = BuildDirectory(entries, metadata.Lcid);
        var content = BuildContent(entries);

        WriteAtomically(outputPath, stream => WriteArchive(stream, directory, content, metadata.Lcid));
    }

    private static void WriteArchive(Stream stream, byte[] directory, byte[] content, int lcid)
    {
        var section0Offset = HeaderLength;
        var directoryOffset = section0Offset + HeaderSection0Length;
        var dataOffset = directoryOffset + directory.Length;
        var fileSize = dataOffset + content.Length;

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        WriteItsfHeader(writer, section0Offset, HeaderSection0Length, directoryOffset, directory.Length, dataOffset, lcid);
        WriteHeaderSection0(writer, fileSize);
        writer.Write(directory);
        writer.Write(content);
    }

    private void WriteAtomically(string outputPath, Action<Stream> writeArchive)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempDirectory = string.IsNullOrEmpty(directory) ? Directory.GetCurrentDirectory() : directory;
        var tempPath = Path.Combine(tempDirectory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        var published = false;

        try
        {
            _writeTempFile(tempPath, writeArchive);
            if (File.Exists(outputPath))
            {
                ReplaceExisting(tempPath, outputPath);
            }
            else
            {
                File.Move(tempPath, outputPath);
            }

            published = true;
        }
        finally
        {
            if (!published)
            {
                TryDelete(tempPath);
            }
        }
    }

    private static void WriteTempFile(string path, Action<Stream> writeArchive)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        writeArchive(stream);
    }

    private static void ReplaceExisting(string sourcePath, string destinationPath)
    {
        try
        {
            File.Replace(sourcePath, destinationPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        catch (PlatformNotSupportedException)
        {
            File.Move(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Preserve the original write/publish exception.
        }
    }

    private static List<ChmEntry> BuildEntries(IReadOnlyList<InputFile> inputFiles, ChmMetadata metadata)
    {
        var entries = new Dictionary<string, ChmEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["::DataSpace/NameList"] = new("::DataSpace/NameList", BuildNameList(), isUserFile: false),
            ["/#SYSTEM"] = new("/#SYSTEM", BuildSystemFile(metadata), isUserFile: false),
            ["/#ITBITS"] = new("/#ITBITS", Array.Empty<byte>(), isUserFile: false)
        };

        var strings = BuildStringsFile(metadata);
        entries["/#STRINGS"] = new ChmEntry("/#STRINGS", strings.Data, isUserFile: false);
        entries["/#WINDOWS"] = new ChmEntry("/#WINDOWS", BuildWindowsFile(metadata, strings), isUserFile: false);

        foreach (var input in inputFiles)
        {
            var name = ArchivePath.ForDirectory(input.ArchivePath);
            if (!entries.ContainsKey(name))
            {
                entries[name] = new ChmEntry(name, input.Data ?? File.ReadAllBytes(input.SourcePath), isUserFile: true);
            }
        }

        return entries.Values.OrderBy(e => e.Name, ChmPathComparer.Instance).ToList();
    }

    private static void AssignContentOffsets(List<ChmEntry> entries)
    {
        long offset = 0;
        foreach (var entry in entries)
        {
            entry.Offset = offset;
            offset += entry.Data.Length;
        }
    }

    private static byte[] BuildContent(List<ChmEntry> entries)
    {
        using var stream = new MemoryStream();
        foreach (var entry in entries)
        {
            stream.Write(entry.Data);
        }

        return stream.ToArray();
    }

    private static byte[] BuildDirectory(List<ChmEntry> entries, int lcid)
    {
        var entryBytes = entries.Select(e => new DirectoryEntryBytes(e.Name, BuildDirectoryEntry(e))).ToList();
        var pmglCount = CountPmglChunks(entryBytes);
        var hasIndex = pmglCount > 1;
        var firstPmglChunkNumber = hasIndex ? 1 : 0;

        var pmglChunks = BuildPmglChunks(entryBytes, firstPmglChunkNumber);
        var chunks = new List<byte[]>();
        if (hasIndex)
        {
            chunks.Add(BuildSinglePmgiChunk(pmglChunks, firstPmglChunkNumber));
        }

        chunks.AddRange(pmglChunks.Select(c => c.Bytes));

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.WriteAscii("ITSP");
        writer.Write(1);
        writer.Write(DirectoryHeaderLength);
        writer.Write(0x0a);
        writer.Write(DirectoryBlockLength);
        writer.Write(QuickRefDensity);
        writer.Write(hasIndex ? 2 : 1);
        writer.Write(hasIndex ? 0 : -1);
        writer.Write(firstPmglChunkNumber);
        writer.Write(firstPmglChunkNumber + pmglChunks.Count - 1);
        writer.Write(-1);
        writer.Write(chunks.Count);
        writer.Write(lcid);
        writer.WriteGuid("5D02926A-212E-11D0-9DF9-00A0C922E6EC");
        writer.Write(DirectoryHeaderLength);
        writer.Write(-1);
        writer.Write(-1);
        writer.Write(-1);

        foreach (var chunk in chunks)
        {
            writer.Write(chunk);
        }

        return stream.ToArray();
    }

    private static int CountPmglChunks(IReadOnlyList<DirectoryEntryBytes> entries)
    {
        var count = 0;
        var current = new List<DirectoryEntryBytes>();
        var currentBytes = 0;
        foreach (var entry in entries)
        {
            EnsureDirectoryEntryFits(entry);

            if (!CanFitPmgl(current.Count + 1, currentBytes + entry.Bytes.Length))
            {
                if (current.Count == 0)
                {
                    throw new CompilationException($"Directory entry is too large for a CHM block: {entry.Name}");
                }

                count++;
                current.Clear();
                currentBytes = 0;
            }

            current.Add(entry);
            currentBytes += entry.Bytes.Length;
        }

        if (current.Count > 0)
        {
            count++;
        }

        return Math.Max(count, 1);
    }

    private static List<PmglChunk> BuildPmglChunks(IReadOnlyList<DirectoryEntryBytes> entries, int firstChunkNumber)
    {
        var groups = new List<List<DirectoryEntryBytes>>();
        var current = new List<DirectoryEntryBytes>();
        var currentBytes = 0;

        foreach (var entry in entries)
        {
            EnsureDirectoryEntryFits(entry);

            if (!CanFitPmgl(current.Count + 1, currentBytes + entry.Bytes.Length))
            {
                if (current.Count == 0)
                {
                    throw new CompilationException($"Directory entry is too large for a CHM block: {entry.Name}");
                }

                groups.Add(current);
                current = new List<DirectoryEntryBytes>();
                currentBytes = 0;
            }

            current.Add(entry);
            currentBytes += entry.Bytes.Length;
        }

        if (current.Count > 0)
        {
            groups.Add(current);
        }

        if (groups.Count == 0)
        {
            groups.Add(new List<DirectoryEntryBytes>());
        }

        var chunks = new List<PmglChunk>();
        for (var i = 0; i < groups.Count; i++)
        {
            var chunkNumber = firstChunkNumber + i;
            var previous = i == 0 ? -1 : chunkNumber - 1;
            var next = i == groups.Count - 1 ? -1 : chunkNumber + 1;
            chunks.Add(new PmglChunk(groups[i].FirstOrDefault()?.Name ?? string.Empty, BuildPmglChunk(groups[i], previous, next)));
        }

        return chunks;
    }

    private static void EnsureDirectoryEntryFits(DirectoryEntryBytes entry)
    {
        if (!CanFitPmgl(1, entry.Bytes.Length))
        {
            throw new CompilationException($"Directory entry is too large for a CHM block: {entry.Name}");
        }
    }

    private static bool CanFitPmgl(int entryCount, int entriesBytes)
    {
        return 0x14 + entriesBytes <= DirectoryBlockLength - QuickRefSize(entryCount);
    }

    private static byte[] BuildPmglChunk(IReadOnlyList<DirectoryEntryBytes> entries, int previous, int next)
    {
        var block = new byte[DirectoryBlockLength];
        var span = block.AsSpan();
        Encoding.ASCII.GetBytes("PMGL").CopyTo(span);
        BinaryUtil.WriteUInt32LittleEndian(span, 8, 0);
        BinaryUtil.WriteInt32LittleEndian(span, 12, previous);
        BinaryUtil.WriteInt32LittleEndian(span, 16, next);

        var pos = 0x14;
        var offsets = new List<int>();
        foreach (var entry in entries)
        {
            offsets.Add(pos);
            entry.Bytes.CopyTo(span[pos..]);
            pos += entry.Bytes.Length;
        }

        WriteQuickRef(span, offsets);
        BinaryUtil.WriteUInt32LittleEndian(span, 4, checked((uint)(DirectoryBlockLength - pos)));
        return block;
    }

    private static byte[] BuildSinglePmgiChunk(IReadOnlyList<PmglChunk> pmglChunks, int firstPmglChunkNumber)
    {
        var entries = new List<DirectoryEntryBytes>();
        for (var i = 0; i < pmglChunks.Count; i++)
        {
            var name = pmglChunks[i].FirstName;
            using var stream = new MemoryStream();
            WriteEncInt(stream, Encoding.UTF8.GetByteCount(name));
            stream.Write(Encoding.UTF8.GetBytes(name));
            WriteEncInt(stream, firstPmglChunkNumber + i);
            entries.Add(new DirectoryEntryBytes(name, stream.ToArray()));
        }

        var entriesBytes = entries.Sum(e => e.Bytes.Length);
        if (0x08 + entriesBytes > DirectoryBlockLength - QuickRefSize(entries.Count))
        {
            throw new CompilationException("The CHM directory is too large for this compiler version; split the project or reduce file count.");
        }

        var block = new byte[DirectoryBlockLength];
        var span = block.AsSpan();
        Encoding.ASCII.GetBytes("PMGI").CopyTo(span);
        var pos = 0x08;
        var offsets = new List<int>();
        foreach (var entry in entries)
        {
            offsets.Add(pos);
            entry.Bytes.CopyTo(span[pos..]);
            pos += entry.Bytes.Length;
        }

        WriteQuickRef(span, offsets);
        BinaryUtil.WriteUInt32LittleEndian(span, 4, checked((uint)(DirectoryBlockLength - pos)));
        return block;
    }

    private static void WriteQuickRef(Span<byte> block, IReadOnlyList<int> entryOffsets)
    {
        var qpos = DirectoryBlockLength - 2;
        BinaryUtil.WriteUInt16LittleEndian(block, qpos, entryOffsets.Count);

        for (var i = QuickRefInterval; i < entryOffsets.Count; i += QuickRefInterval)
        {
            qpos -= 2;
            BinaryUtil.WriteUInt16LittleEndian(block, qpos, entryOffsets[i] - entryOffsets[0]);
        }
    }

    private static int QuickRefSize(int entryCount)
    {
        var quickRefs = entryCount <= 0 ? 0 : (entryCount - 1) / QuickRefInterval;
        return 2 + quickRefs * 2;
    }

    private static byte[] BuildDirectoryEntry(ChmEntry entry)
    {
        var nameBytes = Encoding.UTF8.GetBytes(entry.Name);
        using var stream = new MemoryStream();
        WriteEncInt(stream, nameBytes.Length);
        stream.Write(nameBytes);
        WriteEncInt(stream, 0);
        WriteEncInt(stream, entry.Offset);
        WriteEncInt(stream, entry.Data.Length);
        return stream.ToArray();
    }

    private static void WriteItsfHeader(
        BinaryWriter writer,
        long section0Offset,
        long section0Length,
        long directoryOffset,
        long directoryLength,
        long dataOffset,
        int lcid)
    {
        writer.WriteAscii("ITSF");
        writer.Write(3);
        writer.Write(HeaderLength);
        writer.Write(1);
        writer.Write(unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        writer.Write(lcid);
        writer.WriteGuid("7C01FD10-7BAA-11D0-9E0C-00A0C922E6EC");
        writer.WriteGuid("7C01FD11-7BAA-11D0-9E0C-00A0C922E6EC");
        writer.Write(section0Offset);
        writer.Write(section0Length);
        writer.Write(directoryOffset);
        writer.Write(directoryLength);
        writer.Write(dataOffset);
    }

    private static void WriteHeaderSection0(BinaryWriter writer, long fileSize)
    {
        writer.Write(0x01FE);
        writer.Write(0);
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(0);
    }

    private static byte[] BuildNameList()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)"Uncompressed".Length);
        writer.Write(Encoding.Unicode.GetBytes("Uncompressed"));
        writer.Write((ushort)0);

        var bytes = stream.ToArray();
        BinaryUtil.WriteUInt16LittleEndian(bytes, 0, bytes.Length / 2);
        return bytes;
    }

    private static byte[] BuildSystemFile(ChmMetadata metadata)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(3);

        AddEntry(writer, 10, BitConverter.GetBytes(unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds())));
        AddEntry(writer, 9, NtString($"{VersionInfo.Name} {VersionInfo.Version}", metadata.TextEncoding));
        AddEntry(writer, 4, BuildSystemCode4(metadata));

        if (metadata.DefaultTopic is not null)
        {
            AddEntry(writer, 2, NtString(metadata.DefaultTopic, metadata.TextEncoding));
        }

        AddEntry(writer, 3, NtString(metadata.Title, metadata.TextEncoding));

        if (metadata.DefaultFont is not null)
        {
            AddEntry(writer, 16, NtString(metadata.DefaultFont, metadata.TextEncoding));
        }

        AddEntry(writer, 6, NtString(metadata.CompiledFileStem, metadata.TextEncoding));

        if (metadata.DefaultWindow is not null)
        {
            AddEntry(writer, 5, NtString(metadata.DefaultWindow, metadata.TextEncoding));
        }

        if (metadata.ContentsFile is not null)
        {
            AddEntry(writer, 0, NtString(metadata.ContentsFile, metadata.TextEncoding));
        }

        if (metadata.IndexFile is not null)
        {
            AddEntry(writer, 1, NtString(metadata.IndexFile, metadata.TextEncoding));
        }

        AddEntry(writer, 12, BitConverter.GetBytes(0));
        return stream.ToArray();
    }

    private static StringTable BuildStringsFile(ChmMetadata metadata)
    {
        var table = new StringTable(metadata.TextEncoding);
        _ = table.Add(string.Empty);
        _ = table.Add(metadata.DefaultWindow ?? "main");
        _ = table.Add(metadata.Title);
        if (metadata.ContentsFile is not null)
        {
            _ = table.Add(metadata.ContentsFile);
        }

        if (metadata.IndexFile is not null)
        {
            _ = table.Add(metadata.IndexFile);
        }

        if (metadata.DefaultTopic is not null)
        {
            _ = table.Add(metadata.DefaultTopic);
        }

        return table;
    }

    private static byte[] BuildWindowsFile(ChmMetadata metadata, StringTable strings)
    {
        const int entrySize = 196;
        var data = new byte[8 + entrySize];
        var span = data.AsSpan();
        BinaryUtil.WriteInt32LittleEndian(span, 0, 1);
        BinaryUtil.WriteInt32LittleEndian(span, 4, entrySize);

        var entry = span[8..];
        BinaryUtil.WriteInt32LittleEndian(entry, 0x00, entrySize);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x04, 0);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x08, strings.OffsetOf(metadata.DefaultWindow ?? "main"));

        var validMembers =
            0x00000002  // navigation pane style
            | 0x00000010 // initial position
            | 0x00000020 // navigation pane width
            | 0x00000040 // show state
            | 0x00000100 // toolbar buttons
            | 0x00000200 // navigation pane open/closed
            | 0x00000400 // tab position
            | 0x00001000 // history count
            | 0x00002000; // default pane
        BinaryUtil.WriteInt32LittleEndian(entry, 0x0C, validMembers);

        var navStyle =
            0x00000020  // tri-pane
            | 0x00000040 // no text on toolbar buttons
            | 0x00000100 // sync current topic
            | 0x00002000; // current HTML title in title bar
        BinaryUtil.WriteInt32LittleEndian(entry, 0x10, navStyle);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x14, strings.OffsetOf(metadata.Title));

        BinaryUtil.WriteInt32LittleEndian(entry, 0x20, 100);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x24, 100);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x28, 1000);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x2C, 760);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x30, 1);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x4C, 260);

        if (metadata.ContentsFile is not null)
        {
            BinaryUtil.WriteInt32LittleEndian(entry, 0x60, strings.OffsetOf(metadata.ContentsFile));
        }

        if (metadata.IndexFile is not null)
        {
            BinaryUtil.WriteInt32LittleEndian(entry, 0x64, strings.OffsetOf(metadata.IndexFile));
        }

        if (metadata.DefaultTopic is not null)
        {
            BinaryUtil.WriteInt32LittleEndian(entry, 0x68, strings.OffsetOf(metadata.DefaultTopic));
            BinaryUtil.WriteInt32LittleEndian(entry, 0x6C, strings.OffsetOf(metadata.DefaultTopic));
        }

        const int toolbarButtons =
            0x00000002 // Hide/Show
            | 0x00000004 // Back
            | 0x00000008 // Forward
            | 0x00000010 // Stop
            | 0x00000020 // Refresh
            | 0x00000040 // Home
            | 0x00000800 // Locate
            | 0x00001000 // Options
            | 0x00002000 // Print
            | 0x00100000; // Font
        BinaryUtil.WriteInt32LittleEndian(entry, 0x70, toolbarButtons);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x74, 0);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x78, 0);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x7C, 0);
        BinaryUtil.WriteInt32LittleEndian(entry, 0x98, 30);

        return data;
    }

    private static byte[] BuildSystemCode4(ChmMetadata metadata)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(metadata.Lcid);
        writer.Write(IsDbcs(metadata.TextEncoding) ? 1 : 0);
        writer.Write(metadata.FullTextSearch ? 1 : 0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(DateTime.UtcNow.ToFileTimeUtc());
        writer.Write(0);
        writer.Write(0);
        return stream.ToArray();
    }

    private static bool IsDbcs(Encoding encoding)
    {
        return encoding.CodePage is 932 or 936 or 949 or 950 or 1361;
    }

    private static byte[] NtString(string value, Encoding encoding)
    {
        var bytes = encoding.GetBytes(value);
        var result = new byte[bytes.Length + 1];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        return result;
    }

    private static void AddEntry(BinaryWriter writer, ushort code, byte[] data)
    {
        if (data.Length > ushort.MaxValue)
        {
            throw new CompilationException($"#SYSTEM entry {code} is too large.");
        }

        writer.Write(code);
        writer.Write((ushort)data.Length);
        writer.Write(data);
    }

    private static void WriteEncInt(Stream stream, long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Span<byte> stack = stackalloc byte[10];
        var count = 0;
        do
        {
            stack[count++] = (byte)(value & 0x7F);
            value >>= 7;
        }
        while (value > 0);

        for (var i = count - 1; i >= 0; i--)
        {
            var b = stack[i];
            if (i != 0)
            {
                b |= 0x80;
            }

            stream.WriteByte(b);
        }
    }

    private sealed class ChmEntry
    {
        public ChmEntry(string name, byte[] data, bool isUserFile)
        {
            Name = name;
            Data = data;
            IsUserFile = isUserFile;
        }

        public string Name { get; }
        public byte[] Data { get; }
        public bool IsUserFile { get; }
        public long Offset { get; set; }
    }

    private sealed record DirectoryEntryBytes(string Name, byte[] Bytes);

    private sealed record PmglChunk(string FirstName, byte[] Bytes);

    private sealed class StringTable
    {
        private readonly Encoding _encoding;
        private readonly MemoryStream _stream = new();
        private readonly Dictionary<string, int> _offsets = new(StringComparer.Ordinal);

        public StringTable(Encoding encoding)
        {
            _encoding = encoding;
        }

        public byte[] Data => _stream.ToArray();

        public int Add(string value)
        {
            if (_offsets.TryGetValue(value, out var existing))
            {
                return existing;
            }

            var offset = checked((int)_stream.Position);
            var bytes = _encoding.GetBytes(value);
            _stream.Write(bytes);
            _stream.WriteByte(0);
            _offsets[value] = offset;
            return offset;
        }

        public int OffsetOf(string value)
        {
            return _offsets.TryGetValue(value, out var offset) ? offset : Add(value);
        }
    }
}
