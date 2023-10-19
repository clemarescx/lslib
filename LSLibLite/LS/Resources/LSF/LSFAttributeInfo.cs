namespace LSLibLite.LS.Resources.LSF;

internal class LSFAttributeInfo
{
    #region Members

    /// <summary>
    ///     Index into name hash table
    /// </summary>
    public int NameIndex;

    /// <summary>
    ///     Offset in hash chain
    /// </summary>
    public int NameOffset;

    /// <summary>
    ///     Index of the next attribute in this node
    ///     (-1: this is the last attribute)
    /// </summary>
    public int NextAttributeIndex;

    /// <summary>
    ///     Type of this attribute (see NodeAttribute.DataType)
    /// </summary>
    public uint TypeId;

    /// <summary>
    ///     Length of this attribute
    /// </summary>
    public uint Length;

    /// <summary>
    ///     Absolute position of attribute data in the values section
    /// </summary>
    public uint DataOffset;

    #endregion
}