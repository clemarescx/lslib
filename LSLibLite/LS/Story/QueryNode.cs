namespace LSLibLite.LS.Story;

public abstract class QueryNode : Node
{
    public override void MakeScript(
        TextWriter writer,
        Story story,
        Tuple tuple,
        bool printTypes = false)
    {
        writer.Write("{0}(", Name);
        tuple.MakeScript(writer, story, printTypes);
        writer.WriteLine(")");
    }
}