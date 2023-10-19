namespace LSLibLite.LS.Story;

public class RelOpNode : RelNode
{
    #region Members

    public RelOpType RelOp;
    public sbyte LeftValueIndex;
    public sbyte RightValueIndex;
    public Value LeftValue;
    public Value RightValue;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        LeftValueIndex = reader.ReadSByte();
        RightValueIndex = reader.ReadSByte();

        LeftValue = new Value();
        LeftValue.Read(reader);

        RightValue = new Value();
        RightValue.Read(reader);

        RelOp = (RelOpType)reader.ReadInt32();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(LeftValueIndex);
        writer.Write(RightValueIndex);

        LeftValue.Write(writer);
        RightValue.Write(writer);
        writer.Write((uint)RelOp);
    }

    public override Type NodeType()
    {
        return Type.RelOp;
    }

    public override string TypeName()
    {
        return $"RelOp {RelOp}";
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.Write("    Left Value: ");
        if (LeftValueIndex != -1)
        {
            writer.Write("[Source Column {0}]", LeftValueIndex);
        }
        else
        {
            LeftValue.DebugDump(writer, story);
        }

        writer.WriteLine();

        writer.Write("    Right Value: ");
        if (RightValueIndex != -1)
        {
            writer.Write("[Source Column {0}]", RightValueIndex);
        }
        else
        {
            RightValue.DebugDump(writer, story);
        }

        writer.WriteLine();
    }

    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        var adaptedTuple = AdapterRef.Resolve().Adapt(tuple);
        ParentRef.Resolve().MakeScript(writer, story, adaptedTuple, printTypes);
        writer.WriteLine("AND");

        if (LeftValueIndex != -1)
        {
            adaptedTuple.Logical[LeftValueIndex].MakeScript(writer, story, tuple);
        }
        else
        {
            LeftValue.MakeScript(writer, story, tuple);
        }

        switch (RelOp)
        {
            case RelOpType.Less:
                writer.Write(" < ");
                break;

            case RelOpType.LessOrEqual:
                writer.Write(" <= ");
                break;

            case RelOpType.Greater:
                writer.Write(" > ");
                break;

            case RelOpType.GreaterOrEqual:
                writer.Write(" >= ");
                break;

            case RelOpType.Equal:
                writer.Write(" == ");
                break;

            case RelOpType.NotEqual:
                writer.Write(" != ");
                break;
        }

        if (RightValueIndex != -1)
        {
            adaptedTuple.Logical[RightValueIndex].MakeScript(writer, story, tuple);
        }
        else
        {
            RightValue.MakeScript(writer, story, tuple);
        }

        writer.WriteLine();
    }
}