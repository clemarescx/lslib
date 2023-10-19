using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

/// <summary>
///     V2 attribute extension in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFAttributeEntryV2
{
    /// <summary>
    ///     Name of this attribute
    ///     (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;

    /// <summary>
    ///     6-bit LSB: Type of this attribute (see NodeAttribute.DataType)
    ///     26-bit MSB: Length of this attribute
    /// </summary>
    public UInt32 TypeAndLength;

    /// <summary>
    ///     Index of the node that this attribute belongs to
    ///     Note: These indexes are assigned seemingly arbitrarily, and are not necessarily indices into the node list
    /// </summary>
    public Int32 NodeIndex;

    /// <summary>
    ///     Index into name hash table
    /// </summary>
    public readonly int NameIndex => (int)(NameHashTableIndex >> 16);

    /// <summary>
    ///     Offset in hash chain
    /// </summary>
    public readonly int NameOffset => (int)(NameHashTableIndex & 0xffff);

    /// <summary>
    ///     Type of this attribute (see NodeAttribute.DataType)
    /// </summary>
    public readonly uint TypeId => TypeAndLength & 0x3f;

    /// <summary>
    ///     Length of this attribute
    /// </summary>
    public readonly uint Length => TypeAndLength >> 6;
}