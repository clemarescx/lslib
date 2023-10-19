namespace LSLibLite.LS.Story;

public class FunctionSignature : IOsirisSerializable
{
    #region Members

    public List<byte> OutParamMask;
    public ParameterList Parameters;
    public string? Name;

    #endregion

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        OutParamMask = new List<byte>();
        var outParamBytes = reader.ReadUInt32();
        while (outParamBytes-- > 0)
        {
            OutParamMask.Add(reader.ReadByte());
        }

        Parameters = new ParameterList();
        Parameters.Read(reader);
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);

        writer.Write((uint)OutParamMask.Count);
        foreach (var b in OutParamMask)
        {
            writer.Write(b);
        }

        Parameters.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        writer.Write(Name);
        writer.Write("(");
        for (var i = 0; i < Parameters.Types.Count; i++)
        {
            var type = story.Types[Parameters.Types[i]];
            var isOutParam = (OutParamMask[i >> 3] << (i & 7) & 0x80) == 0x80;
            if (isOutParam)
            {
                writer.Write("out ");
            }

            writer.Write(type.Name);
            if (i < Parameters.Types.Count - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");
    }
}