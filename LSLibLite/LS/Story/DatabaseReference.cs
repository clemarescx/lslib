using LSLibLite.LS.Story;

namespace LSLib.LS.Story;

public class DatabaseReference : OsiReference<Database>
{
    public override Database Resolve()
    {
        return Index == NullReference
            ? null
            : Story.Databases[Index];
    }

    public override void DebugDump(TextWriter writer, LSLibLite.LS.Story.Story story)
    {
        if (!IsValid)
        {
            writer.Write("(None)");
        }
        else
        {
            writer.Write("#{0}", Index);
        }
    }
}