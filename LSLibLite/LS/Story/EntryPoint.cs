namespace LSLibLite.LS.Story;

public enum EntryPoint : uint
{
    // The next node is not an AND/NOT AND expression
    None = 0,

    // This node is on the left side of the next AND/NOT AND expression
    Left = 1,

    // This node is on the right side of the next AND/NOT AND expression
    Right = 2
}