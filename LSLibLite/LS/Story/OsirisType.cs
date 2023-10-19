namespace LSLibLite.LS.Story;

public class OsirisType : IOsirisSerializable
{
    #region Members

    public bool IsBuiltin;
    public byte Index;
    public byte Alias;
    public string? Name;

    #endregion

    public void Read(OsiReader reader)
    {
        Name = reader.ReadString();
        Index = reader.ReadByte();
        IsBuiltin = false;

        if (reader.Ver >= OsiVersion.VerTypeAliases)
        {
            Alias = reader.ReadByte();
        }
        else
        {
            // D:OS 1 only supported string aliases
            Alias = (int)Value.Type_OS1.String;
        }
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Name);
        writer.Write(Index);

        if (writer.Ver >= OsiVersion.VerTypeAliases)
        {
            writer.Write(Alias);
        }
    }

    public static OsirisType MakeBuiltin(byte index, string name)
    {
        var type = new OsirisType
        {
            Index = index,
            Alias = 0,
            Name = name,
            IsBuiltin = true
        };

        return type;
    }

    public void DebugDump(TextWriter writer)
    {
        if (Alias == 0)
        {
            writer.WriteLine("{0}: {1}", Index, Name);
        }
        else
        {
            writer.WriteLine("{0}: {1} (Alias: {2})", Index, Name, Alias);
        }
    }
}