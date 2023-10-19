using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public class NodeEntryItem : IOsirisSerializable
{
    #region Members

    public EntryPoint EntryPoint;
    public GoalReference GoalRef;
    public NodeReference NodeRef;

    #endregion

    public void Read(OsiReader reader)
    {
        NodeRef = reader.ReadNodeRef();
        EntryPoint = (EntryPoint)reader.ReadUInt32();
        GoalRef = reader.ReadGoalRef();
    }

    public void Write(OsiWriter writer)
    {
        NodeRef.Write(writer);
        writer.Write((uint)EntryPoint);
        GoalRef.Write(writer);
    }

    public void DebugDump(TextWriter writer, Story story)
    {
        if (NodeRef.IsValid)
        {
            writer.Write("(");
            NodeRef.DebugDump(writer, story);
            if (GoalRef.IsValid)
            {
                writer.Write(", Entry Point {0}, Goal {1})", EntryPoint, GoalRef.Resolve().Name);
            }
            else
            {
                writer.Write(")");
            }
        }
        else
        {
            writer.Write("(none)");
        }
    }
}