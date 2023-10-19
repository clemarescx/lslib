using System.IO.Hashing;
using System.Text;
using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

public sealed class PackagedFileInfo : AbstractFileInfo, IDisposable
{
    #region Members

    private bool _solid;
    public Stream? PackageStream;
    private Stream? _solidStream;
    private Stream? _uncompressedStream;
    public uint ArchivePart;
    public uint Crc;
    public uint Flags;
    private uint _solidOffset;
    public ulong OffsetInFile;
    public ulong SizeOnDisk;
    public ulong UncompressedSize;

    #endregion

    public void Dispose()
    {
        ReleaseStream();
    }

    public override ulong Size()
    {
        return (Flags & 0x0F) == 0
            ? SizeOnDisk
            : UncompressedSize;
    }

    public override uint CRC()
    {
        return Crc;
    }

    public override Stream? MakeStream()
    {
        if (IsDeletion())
        {
            throw new InvalidOperationException("Cannot open file stream for a deleted file");
        }

        if (_uncompressedStream != null)
        {
            return _uncompressedStream;
        }

        if ((CompressionMethod)(Flags & 0x0F) == CompressionMethod.None && !_solid)
        {
            // Use direct stream read for non-compressed files
            _uncompressedStream = new UncompressedPackagedFileStream(PackageStream, this);
            return _uncompressedStream;
        }

        if (SizeOnDisk > 0x7fffffff)
        {
            throw new InvalidDataException($"File '{Name}' is over 2GB ({SizeOnDisk} bytes), which is not supported yet!");
        }

        var compressed = new byte[SizeOnDisk];

        PackageStream.Seek((long)OffsetInFile, SeekOrigin.Begin);
        var readSize = PackageStream.Read(compressed, 0, (int)SizeOnDisk);
        if (readSize != (long)SizeOnDisk)
        {
            var msg = $"Failed to read {SizeOnDisk} bytes from archive (only got {readSize})";
            throw new InvalidDataException(msg);
        }

        if (Crc != 0)
        {
            var hash = Crc32.Hash(compressed);
            var computedCrc = Crc = BitConverter.ToUInt32(hash);
            if (computedCrc != Crc)
            {
                var msg = $"CRC check failed on file '{Name}', archive is possibly corrupted. Expected {Crc,8:X}, got {computedCrc,8:X}";
                throw new InvalidDataException(msg);
            }
        }

        if (_solid)
        {
            _solidStream.Seek(_solidOffset, SeekOrigin.Begin);
            var uncompressed = new byte[UncompressedSize];
            _solidStream.ReadExactly(uncompressed, 0, (int)UncompressedSize);
            _uncompressedStream = new MemoryStream(uncompressed);
        }
        else
        {
            var uncompressed = BinUtils.Decompress(compressed, (int)Size(), (byte)Flags);
            _uncompressedStream = new MemoryStream(uncompressed);
        }

        return _uncompressedStream;
    }

    public override void ReleaseStream()
    {
        if (_uncompressedStream == null)
        {
            return;
        }

        _uncompressedStream.Dispose();
        _uncompressedStream = null;
    }

    public override bool IsDeletion()
    {
        return OffsetInFile == 0xdeadbeefdeadbeef;
    }

    internal static PackagedFileInfo CreateFromEntry(FileEntry13 entry, Stream dataStream)
    {
        var info = new PackagedFileInfo
        {
            PackageStream = dataStream,
            OffsetInFile = entry.OffsetInFile,
            SizeOnDisk = entry.SizeOnDisk,
            UncompressedSize = entry.UncompressedSize,
            ArchivePart = entry.ArchivePart,
            Flags = entry.Flags,
            Crc = entry.Crc,
            _solid = false
        };

        var nameLen = Array.FindIndex(entry.Name, c => c == 0) is var nullIdx and not -1
            ? nullIdx
            : entry.Name.Length;

        info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

        var compressionMethod = entry.Flags & 0x0F;
        if (compressionMethod <= 2 && (entry.Flags & ~0x7F) == 0)
        {
            return info;
        }

        var msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
        throw new InvalidDataException(msg);
    }

    internal static PackagedFileInfo CreateFromEntry(FileEntry15 entry, Stream dataStream)
    {
        var info = new PackagedFileInfo
        {
            PackageStream = dataStream,
            OffsetInFile = entry.OffsetInFile,
            SizeOnDisk = entry.SizeOnDisk,
            UncompressedSize = entry.UncompressedSize,
            ArchivePart = entry.ArchivePart,
            Flags = entry.Flags,
            Crc = entry.Crc,
            _solid = false
        };

        var nameLen = Array.FindIndex(entry.Name, c => c == 0) is var nullIdx and not -1
            ? nullIdx
            : entry.Name.Length;

        info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

        var compressionMethod = entry.Flags & 0x0F;
        if (compressionMethod <= 2 && (entry.Flags & ~0x7F) == 0)
        {
            return info;
        }

        var msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
        throw new InvalidDataException(msg);
    }

    internal static PackagedFileInfo CreateFromEntry(FileEntry18 entry, Stream dataStream)
    {
        var info = new PackagedFileInfo
        {
            PackageStream = dataStream,
            OffsetInFile = entry.OffsetInFile1 | (ulong)entry.OffsetInFile2 << 32,
            SizeOnDisk = entry.SizeOnDisk,
            UncompressedSize = entry.UncompressedSize,
            ArchivePart = entry.ArchivePart,
            Flags = entry.Flags,
            Crc = 0,
            _solid = false
        };

        var nameLen = Array.FindIndex(entry.Name, c => c == 0) is var nullIdx and not -1
            ? nullIdx
            : entry.Name.Length;

        info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

        var compressionMethod = (uint)entry.Flags & 0x0F;
        if (compressionMethod <= 2 && ((uint)entry.Flags & ~0x7F) == 0)
        {
            return info;
        }

        var msg = $"File '{info.Name}' has unsupported flags: {entry.Flags}";
        throw new InvalidDataException(msg);
    }

    internal static PackagedFileInfo CreateSolidFromEntry(
        FileEntry13 entry,
        Stream dataStream,
        uint solidOffset,
        Stream? solidStream)
    {
        var info = CreateFromEntry(entry, dataStream);
        info._solid = true;
        info._solidOffset = solidOffset;
        info._solidStream = solidStream;
        return info;
    }

    internal static PackagedFileInfo CreateFromEntry(FileEntry7 entry, Stream dataStream)
    {
        var info = new PackagedFileInfo
        {
            PackageStream = dataStream
        };

        var nameLen = Array.FindIndex(entry.Name, c => c == 0) is var nullIdx and not -1
            ? nullIdx
            : entry.Name.Length;

        info.Name = Encoding.UTF8.GetString(entry.Name, 0, nameLen);

        info.OffsetInFile = entry.OffsetInFile;
        info.SizeOnDisk = entry.SizeOnDisk;
        info.UncompressedSize = entry.UncompressedSize;
        info.ArchivePart = entry.ArchivePart;
        info.Crc = 0;

        info.Flags = entry.UncompressedSize > 0
            ? BinUtils.MakeCompressionFlags(CompressionMethod.Zlib, CompressionLevel.DefaultCompression)
            : (uint)0;

        return info;
    }

    internal FileEntry7 MakeEntryV7()
    {
        var entry = new FileEntry7
        {
            Name = new byte[256]
        };

        var encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
        Array.Copy(encodedName, entry.Name, encodedName.Length);

        entry.OffsetInFile = (uint)OffsetInFile;
        entry.SizeOnDisk = (uint)SizeOnDisk;
        entry.UncompressedSize = (Flags & 0x0F) == 0
            ? 0
            : (uint)UncompressedSize;

        entry.ArchivePart = ArchivePart;
        return entry;
    }

    internal FileEntry13 MakeEntryV13()
    {
        var entry = new FileEntry13
        {
            Name = new byte[256]
        };

        var encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
        Array.Copy(encodedName, entry.Name, encodedName.Length);

        entry.OffsetInFile = (uint)OffsetInFile;
        entry.SizeOnDisk = (uint)SizeOnDisk;
        entry.UncompressedSize = (Flags & 0x0F) == 0
            ? 0
            : (uint)UncompressedSize;

        entry.ArchivePart = ArchivePart;
        entry.Flags = Flags;
        entry.Crc = Crc;
        return entry;
    }

    internal FileEntry15 MakeEntryV15()
    {
        var entry = new FileEntry15
        {
            Name = new byte[256],
            OffsetInFile = OffsetInFile,
            SizeOnDisk = SizeOnDisk,
            UncompressedSize = (Flags & 0x0F) == 0
                ? 0
                : UncompressedSize,
            Flags = Flags,
            Crc = Crc,
            ArchivePart = ArchivePart,
            Unknown2 = 0
        };

        var encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
        Array.Copy(encodedName, entry.Name, encodedName.Length);

        return entry;
    }

    internal FileEntry18 MakeEntryV18()
    {
        var entry = new FileEntry18
        {
            Name = new byte[256],
            OffsetInFile1 = (uint)(OffsetInFile & 0xffffffff),
            OffsetInFile2 = (ushort)(OffsetInFile >> 32 & 0xffff),
            SizeOnDisk = (uint)SizeOnDisk,
            UncompressedSize = (Flags & 0x0F) == 0
                ? 0
                : (uint)UncompressedSize,
            Flags = (byte)Flags,
            ArchivePart = (byte)ArchivePart
        };

        var encodedName = Encoding.UTF8.GetBytes(Name.Replace('\\', '/'));
        Array.Copy(encodedName, entry.Name, encodedName.Length);

        return entry;
    }
}