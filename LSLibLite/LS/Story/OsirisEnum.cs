namespace LSLibLite.LS.Story;

public class OsirisEnum : IOsirisSerializable
{
    #region Members

    public List<OsirisEnumElement> Elements;
    public ushort UnderlyingType;

    #endregion

    public void Read(OsiReader reader)
    {
        UnderlyingType = reader.ReadUInt16();
        var elements = reader.ReadUInt32();
        Elements = new List<OsirisEnumElement>();
        while (elements-- > 0)
        {
            var e = new OsirisEnumElement();
            e.Read(reader);
            Elements.Add(e);
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(UnderlyingType);
        writer.Write((uint)Elements.Count);

        foreach (var e in Elements)
        {
            e.Write(writer);
        }
    }

    public void DebugDump(TextWriter writer)
    {
        writer.WriteLine("Type {0}", UnderlyingType);
        foreach (var e in Elements)
        {
            e.DebugDump(writer);
        }
    }
}