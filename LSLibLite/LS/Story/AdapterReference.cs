using LSLibLite.LS.Story;

namespace LSLib.LS.Story;

public class AdapterReference : OsiReference<Adapter>
{
    public override Adapter Resolve()
    {
        return Index == NullReference
            ? null
            : Story.Adapters[Index];
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