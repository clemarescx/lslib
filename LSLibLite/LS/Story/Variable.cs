namespace LSLibLite.LS.Story;

public class Variable : TypedValue
{
    #region Members

    public bool Unused;
    public bool Adapted;
    public sbyte Index;
    public string VariableName;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        Index = reader.ReadSByte();
        Unused = reader.ReadBoolean();
        Adapted = reader.ReadBoolean();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(Index);
        writer.Write(Unused);
        writer.Write(Adapted);
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        writer.Write("#{0} ", Index);
        if (VariableName is { Length: > 0 })
        {
            writer.Write("'{0}' ", VariableName);
        }

        if (Unused)
        {
            writer.Write("unused ");
        }

        if (Adapted)
        {
            writer.Write("adapted ");
        }

        base.DebugDump(writer, story);
    }

    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        if (Unused)
        {
            if (printTypes && TypeId > 0)
            {
                writer.Write("({0})", story.Types[TypeId].Name);
            }

            writer.Write("_");
        }
        else if (Adapted)
        {
            if (VariableName is { Length: > 0 })
            {
                if (printTypes && TypeId > 0)
                {
                    writer.Write("({0})", story.Types[TypeId].Name);
                }

                writer.Write(VariableName);
            }
            else
            {
                tuple.Logical[Index].MakeScript(writer, story, null);
            }
        }
        else
        {
            base.MakeScript(writer, story, tuple, false);
        }
    }
}