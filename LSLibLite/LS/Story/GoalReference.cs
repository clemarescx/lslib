using LSLibLite.LS.Story;

namespace LSLib.LS.Story;

public class GoalReference : OsiReference<Goal>
{
    public GoalReference() { }

    public GoalReference(LSLibLite.LS.Story.Story story, uint reference) : base(story, reference) { }

    public override Goal Resolve()
    {
        return Index == NullReference
            ? null
            : Story.Goals[Index];
    }

    public override void DebugDump(TextWriter writer, LSLibLite.LS.Story.Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            var goal = Resolve();
            writer.Write("#{0} <{1}>", Index, goal.Name);
        }
    }
}