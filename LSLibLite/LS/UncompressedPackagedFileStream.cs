using LSLibLite.LS.Enums;

namespace LSLibLite.LS;

public class UncompressedPackagedFileStream : Stream
{
    #region Members

    private readonly PackagedFileInfo FileInfo;
    private readonly Stream? PackageStream;

    #endregion

    #region Constructors

    public UncompressedPackagedFileStream(Stream? packageStream, PackagedFileInfo fileInfo)
    {
        PackageStream = packageStream;
        FileInfo = fileInfo;
        PackageStream.Seek((long)fileInfo.OffsetInFile, SeekOrigin.Begin);

        if ((CompressionMethod)(FileInfo.Flags & 0x0F) != CompressionMethod.None)
        {
            throw new ArgumentException("We only support uncompressed files!");
        }
    }

    #endregion

    #region Properties

    public override bool CanRead => true;
    public override bool CanSeek => false;

    public override bool CanTimeout => PackageStream.CanTimeout;
    public override bool CanWrite => false;

    public override long Position
    {
        get => PackageStream.Position - (long)FileInfo.OffsetInFile;
        set => throw new NotSupportedException();
    }

    public override long Length => (long)FileInfo.SizeOnDisk;

    #endregion

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (PackageStream.Position < (long)FileInfo.OffsetInFile || PackageStream.Position > (long)FileInfo.OffsetInFile + (long)FileInfo.SizeOnDisk)
        {
            throw new Exception("Stream at unexpected position while reading packaged file?");
        }

        var readable = (long)FileInfo.SizeOnDisk - Position;
        var bytesToRead = readable < count
            ? (int)readable
            : count;

        return PackageStream.Read(buffer, offset, bytesToRead);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value) { throw new NotSupportedException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    public override void Flush() { }
}