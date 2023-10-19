namespace LSLibLite.LS.Story;

public class InternalQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.InternalQuery;
    }

    public override string TypeName()
    {
        return "Internal Query";
    }
}