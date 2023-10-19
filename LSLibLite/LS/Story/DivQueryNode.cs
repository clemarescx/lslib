namespace LSLibLite.LS.Story;

public class DivQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.DivQuery;
    }

    public override string TypeName()
    {
        return "Div Query";
    }
}