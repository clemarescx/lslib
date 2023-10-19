namespace LSLibLite.LS.Story;

public class OsirisDivObject : IOsirisSerializable
{
    #region Members

    public byte Type;
    public string? Name;
    public uint Key1;
    public uint Key2; // Some ref?
    public uint Key3; // Type again?
    public uint Key4;

    #endregion

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        Type = reader.ReadByte();
        Key1 = reader.ReadUInt32();
        Key2 = reader.ReadUInt32();
        Key3 = reader.ReadUInt32();
        Key4 = reader.ReadUInt32();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        writer.Write(Type);
        writer.Write(Key1);
        writer.Write(Key2);
        writer.Write(Key3);
        writer.Write(Key4);
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine(
            "{0} {1} ({2}, {3}, {4}, {5})",
            Type,
            Name,
            Key1,
            Key2,
            Key3,
            Key4);
    }
}