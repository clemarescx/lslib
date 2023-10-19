namespace LSLibLite.LS.Story;

public class UserQueryNode : QueryNode
{
    public override Type NodeType()
    {
        return Type.UserQuery;
    }

    public override string TypeName()
    {
        return "User Query";
    }
}