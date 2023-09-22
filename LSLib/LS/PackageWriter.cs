using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LSLib.LS.Enums;
using LSLib.Native;
using LZ4;

namespace LSLib.LS;

public sealed class PackageWriter : IDisposable
{
    public delegate void WriteProgressDelegate(AbstractFileInfo abstractFile, long numerator, long denominator);

    private const long MaxPackageSizeDOS = 0x40000000;
    private const long MaxPackageSizeBG3 = 0x100000000;
    public CompressionMethod Compression = CompressionMethod.None;
    public CompressionLevel CompressionLevel = CompressionLevel.DefaultCompression;

    private readonly Package _package;
    private readonly string _path;
    private readonly List<Stream> _streams = new();
    public PackageVersion Version = Package.CurrentVersion;
    public WriteProgressDelegate WriteProgress = delegate { };

    public PackageWriter(Package package, string path)
    {
        _package = package;
        _path = path;
    }

    public void Dispose()
    {
        foreach (var stream in _streams)
        {
            stream.Dispose();
        }
    }

    private int PaddingLength() =>
        Version <= PackageVersion.V9
            ? 0x8000
            : 0x40;

    private PackagedFileInfo WriteFile(AbstractFileInfo info)
    {
        // Assume that all files are written uncompressed (worst-case) when calculating package sizes
        var size = (long)info.Size();
        if (Version < PackageVersion.V15 && _streams[^1].Position + size > MaxPackageSizeDOS
         || Version >= PackageVersion.V16 && _streams[^1].Position + size > MaxPackageSizeBG3)
        {
            // Start a new package file if the current one is full.
            var partPath = Package.MakePartFilename(_path, _streams.Count);
            var nextPart = File.Open(partPath, FileMode.Create, FileAccess.Write);
            _streams.Add(nextPart);
        }

        var stream = _streams[^1];
        var packaged = new PackagedFileInfo
        {
            PackageStream = stream,
            Name = info.Name,
            UncompressedSize = (ulong)size,
            ArchivePart = (uint)(_streams.Count - 1),
            OffsetInFile = (uint)stream.Position,
            Flags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel)
        };

        var packagedStream = info.MakeStream();
        byte[] compressed;
        try
        {
            using var reader = new BinaryReader(packagedStream, Encoding.UTF8, true);
            var uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
            compressed = BinUtils.Compress(uncompressed, Compression, CompressionLevel);
            stream.Write(compressed, 0, compressed.Length);
        }
        finally
        {
            info.ReleaseStream();
        }

        packaged.SizeOnDisk = (ulong)(stream.Position - (long)packaged.OffsetInFile);
        packaged.Crc = Crc32.Compute(compressed, 0);

        if ((_package.Metadata.Flags & PackageFlags.Solid) != 0)
        {
            return packaged;
        }

        var padLength = PaddingLength();
        long alignTo;
        if (Version >= PackageVersion.V16)
        {
            alignTo = stream.Position - Marshal.SizeOf(typeof(LSPKHeader16)) - 4;
        }
        else
        {
            alignTo = stream.Position;
        }

        // Pad the file to a multiple of 64 bytes
        var padBytes = (padLength - alignTo % padLength) % padLength;
        var pad = new byte[padBytes];
        for (var i = 0; i < pad.Length; i++)
        {
            pad[i] = 0xAD;
        }

        stream.Write(pad, 0, pad.Length);

        return packaged;
    }

    private void WriteV7(Stream mainStream)
    {
        if (Compression == CompressionMethod.LZ4)
        {
            throw new ArgumentException("LZ4 compression is only supported by V10 and later package versions");
        }

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);
        var header = new LSPKHeader7
        {
            Version = (uint)Version,
            NumFiles = (uint)_package.Files.Count,
            FileListSize = (uint)(Marshal.SizeOf(typeof(FileEntry7)) * _package.Files.Count)
        };

        header.DataOffset = (uint)Marshal.SizeOf(typeof(LSPKHeader7)) + header.FileListSize;
        var paddingLength = PaddingLength();
        if (header.DataOffset % paddingLength > 0)
        {
            header.DataOffset += (uint)(paddingLength - header.DataOffset % paddingLength);
        }

        // Write a placeholder instead of the actual headers; we'll write them after we
        // compressed and flushed all files to disk
        var placeholder = new byte[header.DataOffset];
        writer.Write(placeholder);

        var totalSize = _package.Files.Sum(p => (long)p.Size());
        long currentSize = 0;
        var writtenFiles = new List<PackagedFileInfo>();
        foreach (var file in _package.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += (long)file.Size();
        }

        mainStream.Seek(0, SeekOrigin.Begin);
        header.LittleEndian = 0;
        header.NumParts = (ushort)_streams.Count;
        BinUtils.WriteStruct(writer, ref header);

        foreach (var file in writtenFiles)
        {
            var entry = file.MakeEntryV7();
            if (entry.ArchivePart == 0)
            {
                entry.OffsetInFile -= header.DataOffset;
            }

            BinUtils.WriteStruct(writer, ref entry);
        }
    }

    private void WriteV10(Stream mainStream)
    {
        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);
        var header = new LSPKHeader10
        {
            Version = (uint)Version,
            NumFiles = (uint)_package.Files.Count,
            FileListSize = (uint)(Marshal.SizeOf(typeof(FileEntry13)) * _package.Files.Count)
        };

        header.DataOffset = (uint)Marshal.SizeOf(typeof(LSPKHeader10)) + 4 + header.FileListSize;
        var paddingLength = PaddingLength();
        if (header.DataOffset % paddingLength > 0)
        {
            header.DataOffset += (uint)(paddingLength - header.DataOffset % paddingLength);
        }

        // Write a placeholder instead of the actual headers; we'll write them after we
        // compressed and flushed all files to disk
        var placeholder = new byte[header.DataOffset];
        writer.Write(placeholder);

        var totalSize = _package.Files.Sum(p => (long)p.Size());
        long currentSize = 0;
        var writtenFiles = new List<PackagedFileInfo>();
        foreach (var file in _package.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += (long)file.Size();
        }

        mainStream.Seek(0, SeekOrigin.Begin);
        writer.Write(Package.Signature);
        header.NumParts = (ushort)_streams.Count;
        header.Priority = _package.Metadata.Priority;
        header.Flags = (byte)_package.Metadata.Flags;
        BinUtils.WriteStruct(writer, ref header);

        foreach (var file in writtenFiles)
        {
            var entry = file.MakeEntryV13();
            if (entry.ArchivePart == 0)
            {
                entry.OffsetInFile -= header.DataOffset;
            }

            // v10 packages don't support compression level in the flags field
            entry.Flags &= 0x0f;
            BinUtils.WriteStruct(writer, ref entry);
        }
    }

    private void WriteV13(Stream mainStream)
    {
        var totalSize = _package.Files.Sum(p => (long)p.Size());
        long currentSize = 0;

        var writtenFiles = new List<PackagedFileInfo>();
        foreach (var file in _package.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += (long)file.Size();
        }

        using var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true);
        var header = new LSPKHeader13
        {
            Version = (uint)Version,
            FileListOffset = (uint)mainStream.Position
        };

        writer.Write((uint)writtenFiles.Count);

        var fileList = new MemoryStream();
        var fileListWriter = new BinaryWriter(fileList);
        foreach (var file in writtenFiles)
        {
            var entry = file.MakeEntryV13();
            BinUtils.WriteStruct(fileListWriter, ref entry);
        }

        var fileListBuf = fileList.ToArray();
        fileListWriter.Dispose();
        var compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

        writer.Write(compressedFileList);

        header.FileListSize = (uint)mainStream.Position - header.FileListOffset;
        header.NumParts = (ushort)_streams.Count;
        header.Priority = _package.Metadata.Priority;
        header.Flags = (byte)_package.Metadata.Flags;
        header.Md5 = ComputeArchiveHash();
        BinUtils.WriteStruct(writer, ref header);

        writer.Write((uint)(8 + Marshal.SizeOf(typeof(LSPKHeader13))));
        writer.Write(Package.Signature);
    }

    private List<PackagedFileInfo> PackFiles()
    {
        var totalSize = _package.Files.Sum(p => (long)p.Size());
        long currentSize = 0;

        var writtenFiles = new List<PackagedFileInfo>();
        foreach (var file in _package.Files)
        {
            WriteProgress(file, currentSize, totalSize);
            writtenFiles.Add(WriteFile(file));
            currentSize += (long)file.Size();
        }

        return writtenFiles;
    }

    private static void WriteFileListV15(BinaryWriter metadataWriter, List<PackagedFileInfo> files)
    {
        byte[] fileListBuf;
        using (var fileList = new MemoryStream())
        using (var fileListWriter = new BinaryWriter(fileList))
        {
            foreach (var file in files)
            {
                var entry = file.MakeEntryV15();
                BinUtils.WriteStruct(fileListWriter, ref entry);
            }

            fileListBuf = fileList.ToArray();
        }

        var compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

        metadataWriter.Write((uint)files.Count);
        metadataWriter.Write((uint)compressedFileList.Length);
        metadataWriter.Write(compressedFileList);
    }

    private void WriteV15(Stream mainStream)
    {
        var header = new LSPKHeader15
        {
            Version = (uint)Version
        };

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            writer.Write(Package.Signature);
            BinUtils.WriteStruct(writer, ref header);
        }

        var writtenFiles = PackFiles();

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            header.FileListOffset = (ulong)mainStream.Position;
            WriteFileListV15(writer, writtenFiles);

            header.FileListSize = (uint)(mainStream.Position - (long)header.FileListOffset);
            header.Priority = _package.Metadata.Priority;
            header.Flags = (byte)_package.Metadata.Flags;
            header.Md5 = ComputeArchiveHash();
            mainStream.Seek(4, SeekOrigin.Begin);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    private void WriteV16(Stream mainStream)
    {
        var header = new LSPKHeader16
        {
            Version = (uint)Version
        };

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            writer.Write(Package.Signature);
            BinUtils.WriteStruct(writer, ref header);
        }

        var writtenFiles = PackFiles();

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            header.FileListOffset = (ulong)mainStream.Position;
            WriteFileListV15(writer, writtenFiles);

            header.FileListSize = (uint)(mainStream.Position - (long)header.FileListOffset);
            header.Priority = _package.Metadata.Priority;
            header.Flags = (byte)_package.Metadata.Flags;
            header.Md5 = ComputeArchiveHash();
            header.NumParts = (ushort)_streams.Count;
            mainStream.Seek(4, SeekOrigin.Begin);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    private static void WriteFileListV18(BinaryWriter metadataWriter, List<PackagedFileInfo> files)
    {
        byte[] fileListBuf;
        using (var fileList = new MemoryStream())
        using (var fileListWriter = new BinaryWriter(fileList))
        {
            foreach (var file in files)
            {
                var entry = file.MakeEntryV18();
                BinUtils.WriteStruct(fileListWriter, ref entry);
            }

            fileListBuf = fileList.ToArray();
        }

        var compressedFileList = LZ4Codec.EncodeHC(fileListBuf, 0, fileListBuf.Length);

        metadataWriter.Write((uint)files.Count);
        metadataWriter.Write((uint)compressedFileList.Length);
        metadataWriter.Write(compressedFileList);
    }

    private void WriteV18(Stream mainStream)
    {
        var header = new LSPKHeader16
        {
            Version = (uint)Version
        };

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            writer.Write(Package.Signature);
            BinUtils.WriteStruct(writer, ref header);
        }

        var writtenFiles = PackFiles();

        using (var writer = new BinaryWriter(mainStream, new UTF8Encoding(), true))
        {
            header.FileListOffset = (ulong)mainStream.Position;
            WriteFileListV18(writer, writtenFiles);

            header.FileListSize = (uint)(mainStream.Position - (long)header.FileListOffset);
            header.Priority = _package.Metadata.Priority;
            header.Flags = (byte)_package.Metadata.Flags;
            header.Md5 = ComputeArchiveHash();
            header.NumParts = (ushort)_streams.Count;
            mainStream.Seek(4, SeekOrigin.Begin);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    private byte[] ComputeArchiveHash()
    {
        // MD5 is computed over the contents of all files in an alphabetically sorted order
        var orderedFileList = _package.Files.Select(item => item).ToList();
        if (Version < PackageVersion.V15)
        {
            orderedFileList.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        using var md5 = MD5.Create();
        foreach (var file in orderedFileList)
        {
            var packagedStream = file.MakeStream();
            try
            {
                using var reader = new BinaryReader(packagedStream);
                var uncompressed = reader.ReadBytes((int)reader.BaseStream.Length);
                md5.TransformBlock(uncompressed, 0, uncompressed.Length, uncompressed, 0);
            }
            finally
            {
                file.ReleaseStream();
            }
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = md5.Hash;

        // All hash bytes are incremented by 1
        for (var i = 0; i < hash.Length; i++)
        {
            hash[i] += 1;
        }

        return hash;
    }

    public void Write()
    {
        var mainStream = File.Open(_path, FileMode.Create, FileAccess.Write);
        _streams.Add(mainStream);

        switch (Version)
        {
            case PackageVersion.V18:
            {
                WriteV18(mainStream);
                break;
            }

            case PackageVersion.V16:
            {
                WriteV16(mainStream);
                break;
            }

            case PackageVersion.V15:
            {
                WriteV15(mainStream);
                break;
            }

            case PackageVersion.V13:
            {
                WriteV13(mainStream);
                break;
            }

            case PackageVersion.V10:
            {
                WriteV10(mainStream);
                break;
            }

            case PackageVersion.V9:
            case PackageVersion.V7:
            {
                WriteV7(mainStream);
                break;
            }

            default:
            {
                throw new ArgumentException($"Cannot write version {Version} packages");
            }
        }
    }
}