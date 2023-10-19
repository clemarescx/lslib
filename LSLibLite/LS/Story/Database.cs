namespace LSLibLite.LS.Story;

public class Database : IOsirisSerializable
{
    #region Members

    public FactCollection Facts;
    public long FactsPosition;
    public Node? OwnerNode;
    public ParameterList Parameters;
    public uint Index;

    #endregion

    public void Read(OsiReader reader)
    {
        Index = reader.ReadUInt32();
        Parameters = new ParameterList();
        Parameters.Read(reader);

        FactsPosition = reader.BaseStream.Position;
        Facts = new FactCollection(this, reader.Story);
        reader.ReadList(Facts);
    }

    public void Write(OsiWriter writer)
    {
        Parameters.Write(writer);
        writer.WriteList(Facts);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (OwnerNode != null)
        {
            if (OwnerNode.Name is { Length: > 0 })
            {
                writer.Write("{0}({1})", OwnerNode.Name, OwnerNode.NumParams);
            }
            else 
            {
                writer.Write("<{0}>", OwnerNode.TypeName());
            }
        }
        else
        {
            writer.Write("(Not owned)");
        }

        writer.Write(" @ {0:X}: ", FactsPosition);
        Parameters.DebugDump(writer, story);

        writer.WriteLine("");
        writer.WriteLine("    Facts: ");
        foreach (var fact in Facts)
        {
            writer.Write("        ");
            fact.DebugDump(writer, story);
            writer.WriteLine();
        }
    }
}