using LSLib.LS.Story;

namespace LSLibLite.LS.Story;

public abstract class DataNode : Node
{
    #region Members

    public List<NodeEntryItem> ReferencedBy;

    #endregion

    public override void Read(OsiReader reader)
    {
        base.Read(reader);
        ReferencedBy = reader.ReadList<NodeEntryItem>();
    }

    public override void Write(OsiWriter writer)
    {
        base.Write(writer);
        writer.WriteList(ReferencedBy);
    }

    public override void PostLoad(Story story)
    {
        base.PostLoad(story);

        foreach (var reference in ReferencedBy)
        {
            if (!reference.NodeRef.IsValid)
            {
                continue;
            }

            var ruleNode = reference.NodeRef.Resolve();
            if (!reference.GoalRef.IsNull && ruleNode is RuleNode node)
            {
                node.DerivedGoalRef = new GoalReference(story, reference.GoalRef.Index);
            }
        }
    }

    public override void DebugDump(TextWriter writer, Story story)
    {
        base.DebugDump(writer, story);

        if (ReferencedBy.Count <= 0)
        {
            return;
        }

        writer.WriteLine("    Referenced By:");
        foreach (var entry in ReferencedBy)
        {
            writer.Write("        ");
            entry.DebugDump(writer, story);
            writer.WriteLine("");
        }
    }
}