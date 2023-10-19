namespace LSLibLite.LS.Resources.LSF;

/// <summary>
///     Processed node information for a node in the LSF file
/// </summary>
internal class LSFNodeInfo
{
    #region Members

    /// <summary>
    ///     Index of the parent node
    ///     (-1: this node is a root region)
    /// </summary>
    public int ParentIndex;

    /// <summary>
    ///     Index into name hash table
    /// </summary>
    public int NameIndex;

    /// <summary>
    ///     Offset in hash chain
    /// </summary>
    public int NameOffset;

    /// <summary>
    ///     Index of the first attribute of this node
    ///     (-1: node has no attributes)
    /// </summary>
    public int FirstAttributeIndex;

    #endregion
}