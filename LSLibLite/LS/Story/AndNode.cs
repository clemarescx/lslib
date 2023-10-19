namespace LSLibLite.LS.Story;

public class AndNode : JoinNode
{
    public override Type NodeType()
    {
        return Type.And;
    }

    public override string TypeName()
    {
        return "And";
    }

    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
        LeftParentRef.Resolve().MakeScript(writer, story, leftTuple, printTypes);
        writer.WriteLine("AND");
        var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
        RightParentRef.Resolve().MakeScript(writer, story, rightTuple);
    }
}