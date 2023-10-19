using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public abstract class TreeNode : Node
{
    #region Members

    public NodeEntryItem NextNode;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        NextNode = new NodeEntryItem();
        NextNode.Read(reader);
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        NextNode.Write(writer);
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);

        if (!NextNode.NodeRef.IsValid)
        {
            return;
        }

        var nextNode = NextNode.NodeRef.Resolve();
        if (nextNode is RuleNode node)
        {
            node.DerivedGoalRef = new GoalReference(story, NextNode.GoalRef.Index);
        }
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        writer.Write("    Next: ");
        NextNode.DebugDump(writer, story);
        writer.WriteLine("");
    }
}