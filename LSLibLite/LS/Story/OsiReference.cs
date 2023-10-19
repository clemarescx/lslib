using LSLibLite.LS.Story;

namespace LSLib.LS.Story;

public abstract class OsiReference<T> : IOsirisSerializable
{
    protected const uint NullReference = 0;

    // TODO: hide!
    public uint Index = NullReference;
    protected LSLibLite.LS.Story.Story? Story;

    public bool IsNull => Index == NullReference;

    public bool IsValid => Index != NullReference;

    protected OsiReference() { }

    protected OsiReference(LSLibLite.LS.Story.Story story, uint reference)
    {
        Story = story;
        Index = reference;
    }

    public void BindStory(LSLibLite.LS.Story.Story story)
    {
        if (Story == null)
        {
            Story = story;
        }
        else
        {
            throw new InvalidOperationException("Reference already bound to a story!");
        }
    }

    public void Read(OsiReader reader)
    {
        Index = reader.ReadUInt32();
    }

    public void Write(OsiWriter writer)
    {
        writer.Write(Index);
    }

    public abstract T Resolve();

    public abstract void DebugDump(TextWriter writer, LSLibLite.LS.Story.Story story);
}