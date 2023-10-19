using LSLibLite.LS.Story;

namespace LSLib.LS.Story;

public class NodeReference : OsiReference<Node>
{
    public override Node Resolve()
    {
        return Index == NullReference
            ? null
            : Story.Nodes[Index];
    }

    public override void DebugDump(TextWriter writer, LSLibLite.LS.Story.Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            var node = Resolve();
            if (node.Name.Length > 0)
            {
                writer.Write("#{0} <{1}({2}) {3}>", Index, node.Name, node.NumParams, node.TypeName());
            }
            else
            {
                writer.Write("#{0} <{1}>", Index, node.TypeName());
            }
        }
    }
}