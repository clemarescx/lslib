namespace LSLibLite.LS;

public sealed class FilesystemFileInfo : AbstractFileInfo, IDisposable
{
    #region Members

    private FileStream? _stream;
    public long CachedSize;
    public string FilesystemPath;

    #endregion

    public void Dispose()
    {
        ReleaseStream();
    }

    public override ulong Size()
    {
        return (ulong)CachedSize;
    }

    public override uint CRC()
    {
        throw new NotImplementedException("!");
    }

    public override Stream MakeStream()
    {
        return _stream ??= File.Open(FilesystemPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public override void ReleaseStream()
    {
        if (_stream == null)
        {
            return;
        }

        _stream.Dispose();
        _stream = null;
    }

    public static FilesystemFileInfo CreateFromEntry(string filesystemPath, string name)
    {
        var info = new FilesystemFileInfo
        {
            Name = name,
            FilesystemPath = filesystemPath
        };

        var fsInfo = new FileInfo(filesystemPath);
        info.CachedSize = fsInfo.Length;
        return info;
    }

    public override bool IsDeletion()
    {
        return false;
    }
}