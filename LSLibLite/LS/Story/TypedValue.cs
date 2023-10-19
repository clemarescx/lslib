namespace LSLibLite.LS.Story;

public class TypedValue : Value
{
    #region Members

    public bool IsValid;
    public bool OutParam;
    public bool IsAType;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        IsValid = reader.ReadBoolean();
        OutParam = reader.ReadBoolean();
        IsAType = reader.ReadBoolean();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.Write(IsValid);
        writer.Write(OutParam);
        writer.Write(IsAType);
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        if (IsValid)
        {
            writer.Write("valid ");
        }

        if (OutParam)
        {
            writer.Write("out ");
        }

        if (IsAType)
        {
            writer.Write("type ");
        }

        if (IsValid)
        {
            base.DebugDump(writer, story);
        }
        else
        {
            writer.Write("<{0}>", story.Types[TypeId].Name);
        }
    }
}