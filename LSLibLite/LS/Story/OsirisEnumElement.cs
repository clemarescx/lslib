namespace LSLibLite.LS.Story;

public class OsirisEnumElement : IOsirisSerializable
{
    #region Members

    public string? Name;
    public ulong Value;

    #endregion

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        Value = reader.ReadUInt64();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        writer.Write(Value);
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine("{0}: {1}", Name, Value);
    }
}