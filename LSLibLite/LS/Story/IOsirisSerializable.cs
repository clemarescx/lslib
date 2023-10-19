namespace LSLibLite.LS.Story;

public interface IOsirisSerializable
{
    void Read(OsiReader reader);
    void Write(OsiWriter writer);
}