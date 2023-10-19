namespace LSLibLite.LS;

public class StreamFileInfo : AbstractFileInfo
{
    #region Members

    private Stream? _stream;

    #endregion

    public override ulong Size()
    {
        return (ulong)_stream.Length;
    }

    public override uint CRC()
    {
        throw new NotImplementedException("!");
    }

    public override Stream? MakeStream()
    {
        return _stream;
    }

    public override void ReleaseStream() { }

    public static StreamFileInfo CreateFromStream(Stream stream, string name)
    {
        var info = new StreamFileInfo
        {
            Name = name,
            _stream = stream
        };

        return info;
    }

    public override bool IsDeletion()
    {
        return false;
    }
}