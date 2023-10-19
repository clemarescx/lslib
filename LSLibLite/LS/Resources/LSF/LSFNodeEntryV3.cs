﻿using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

/// <summary>
///     Node (structure) entry in the LSF file
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFNodeEntryV3
{
    /// <summary>
    ///     Name of this node
    ///     (16-bit MSB: index into name hash table, 16-bit LSB: offset in hash chain)
    /// </summary>
    public UInt32 NameHashTableIndex;

    /// <summary>
    ///     Index of the parent node
    ///     (-1: this node is a root region)
    /// </summary>
    public Int32 ParentIndex;

    /// <summary>
    ///     Index of the next sibling of this node
    ///     (-1: this is the last node)
    /// </summary>
    public Int32 NextSiblingIndex;

    /// <summary>
    ///     Index of the first attribute of this node
    ///     (-1: node has no attributes)
    /// </summary>
    public Int32 FirstAttributeIndex;

    /// <summary>
    ///     Index into name hash table
    /// </summary>
    public int NameIndex => (int)(NameHashTableIndex >> 16);

    /// <summary>
    ///     Offset in hash chain
    /// </summary>
    public int NameOffset => (int)(NameHashTableIndex & 0xffff);
}