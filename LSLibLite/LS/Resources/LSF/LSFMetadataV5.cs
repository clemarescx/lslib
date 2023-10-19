using System.Runtime.InteropServices;

namespace LSLibLite.LS.Resources.LSF;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LSFMetadataV5
{
    /// <summary>
    ///     Total uncompressed size of the string hash table
    /// </summary>
    public UInt32 StringsUncompressedSize;

    /// <summary>
    ///     Compressed size of the string hash table
    /// </summary>
    public UInt32 StringsSizeOnDisk;

    /// <summary>
    ///     Total uncompressed size of the node list
    /// </summary>
    public UInt32 NodesUncompressedSize;

    /// <summary>
    ///     Compressed size of the node list
    /// </summary>
    public UInt32 NodesSizeOnDisk;

    /// <summary>
    ///     Total uncompressed size of the attribute list
    /// </summary>
    public UInt32 AttributesUncompressedSize;

    /// <summary>
    ///     Compressed size of the attribute list
    /// </summary>
    public UInt32 AttributesSizeOnDisk;

    /// <summary>
    ///     Total uncompressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesUncompressedSize;

    /// <summary>
    ///     Compressed size of the raw value buffer
    /// </summary>
    public UInt32 ValuesSizeOnDisk;

    /// <summary>
    ///     Compression method and level used for the string, node, attribute and value buffers.
    ///     Uses the same format as packages (see BinUtils.MakeCompressionFlags)
    /// </summary>
    public Byte CompressionFlags;

    /// <summary>
    ///     Possibly unused, always 0
    /// </summary>
    public Byte Unknown2;

    public UInt16 Unknown3;

    /// <summary>
    ///     Extended node/attribute format indicator, 0 for V2, 0/1 for V3
    /// </summary>
    public UInt32 HasSiblingData;
}