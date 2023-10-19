namespace LSLibLite.LS.Story;

public class NotAndNode : JoinNode
{
    public override Type NodeType()
    {
        return Type.NotAnd;
    }

    public override string TypeName()
    {
        return "Not And";
    }

    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        var leftTuple = LeftAdapterRef.Resolve().Adapt(tuple);
        LeftParentRef.Resolve().MakeScript(writer, story, leftTuple, printTypes);
        writer.WriteLine("AND NOT");
        var rightTuple = RightAdapterRef.Resolve().Adapt(tuple);
        RightParentRef.Resolve().MakeScript(writer, story, rightTuple);
    }
}