// #define DEBUG_LSF_SERIALIZATION
// #define DUMP_LSF_SERIALIZATION

using System.Text;
using LSLibLite.LS.Enums;

namespace LSLibLite.LS.Resources.LSF;

public sealed class LSFReader : ILSReader
{
    #region Members

    private const string DeserializedValuesBinFileName = "values.bin";

    /// <summary>
    ///     Static string hash map
    /// </summary>
    private List<List<string?>> _names;

    /// <summary>
    ///     Preprocessed list of node attributes
    /// </summary>
    private List<LSFAttributeInfo> _attributes;

    /// <summary>
    ///     Preprocessed list of nodes (structures)
    /// </summary>
    private List<LSFNodeInfo> _nodeInfos;

    /// <summary>
    ///     Node instances
    /// </summary>
    private List<Node?> _nodeInstances;

    private LSFMetadataV6 _metadata;

    /// <summary>
    ///     Version of the file we're serializing
    /// </summary>
    private LSFVersion _version;

    /// <summary>
    ///     Game version that generated the LSF file
    /// </summary>
    private PackedVersion _gameVersion;

    /// <summary>
    ///     Input stream
    /// </summary>
    private readonly Stream? _stream;

    /// <summary>
    ///     Raw value data stream
    /// </summary>
    private Stream _values;

    #endregion

    #region Constructors

    public LSFReader(Stream? stream)
    {
        _stream = stream;
    }

    #endregion

    public void Dispose()
    {
        _stream.Dispose();
    }

    public Resource Read()
    {
        using var reader = new BinaryReader(_stream);
        ReadHeaders(reader);

        _names = new List<List<string?>>();
        var namesStream = Decompress(reader, _metadata.StringsSizeOnDisk, _metadata.StringsUncompressedSize, "strings.bin", false);
        using (namesStream)
        {
            ReadNames(namesStream);
        }

        _nodeInfos = new List<LSFNodeInfo>();
        var nodesStream = Decompress(reader, _metadata.NodesSizeOnDisk, _metadata.NodesUncompressedSize, "nodes.bin", true);
        using (nodesStream)
        {
            var longNodes = _version >= LSFVersion.VerExtendedNodes && _metadata.HasSiblingData == 1;
            ReadNodes(nodesStream, longNodes);
        }

        _attributes = new List<LSFAttributeInfo>();
        var attributesStream = Decompress(reader, _metadata.AttributesSizeOnDisk, _metadata.AttributesUncompressedSize, "attributes.bin", true);
        using (attributesStream)
        {
            var hasSiblingData = _version >= LSFVersion.VerExtendedNodes && _metadata.HasSiblingData == 1;
            if (hasSiblingData)
            {
                ReadAttributesV3(attributesStream);
            }
            else
            {
                ReadAttributesV2(attributesStream);
            }
        }

        _values = Decompress(reader, _metadata.ValuesSizeOnDisk, _metadata.ValuesUncompressedSize, DeserializedValuesBinFileName, true);

        Resource resource = new();
        ReadRegions(resource);

        resource.Metadata.MajorVersion = _gameVersion.Major;
        resource.Metadata.MinorVersion = _gameVersion.Minor;
        resource.Metadata.Revision = _gameVersion.Revision;
        resource.Metadata.BuildNumber = _gameVersion.Build;

        return resource;
    }

    /// <summary>
    ///     Reads the static string hash table from the specified stream.
    /// </summary>
    /// <param name="s">Stream to read the hash table from</param>
    private void ReadNames(Stream s)
    {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF NAME TABLE -----");
#endif

        // Format:
        // 32-bit hash entry count (N)
        //     N x 16-bit chain length (L)
        //         L x 16-bit string length (S)
        //             [S bytes of UTF-8 string data]

        using var reader = new BinaryReader(s);
        var numHashEntries = reader.ReadUInt32();
        while (numHashEntries-- > 0)
        {
            var hash = new List<string?>();
            _names.Add(hash);

            var numStrings = reader.ReadUInt16();
            while (numStrings-- > 0)
            {
                var nameLen = reader.ReadUInt16();
                var bytes = reader.ReadBytes(nameLen);
                var name = Encoding.UTF8.GetString(bytes);
                hash.Add(name);
#if DEBUG_LSF_SERIALIZATION
                        Console.WriteLine(String.Format("{0,3:X}/{1}: {2}", Names.Count - 1, hash.Count - 1, name));
#endif
            }
        }
    }

    /// <summary>
    ///     Reads the structure headers for the LSOF resource
    /// </summary>
    /// <param name="s">Stream to read the node headers from</param>
    /// <param name="longNodes">Use the long (V3) on-disk node format</param>
    private void ReadNodes(Stream s, bool longNodes)
    {
#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(" ----- DUMP OF NODE TABLE -----");
            var index = 0;
#endif

        using var reader = new BinaryReader(s);
        while (s.Position < s.Length)
        {
            var resolved = new LSFNodeInfo();
#if DEBUG_LSF_SERIALIZATION
                    var pos = s.Position;
#endif

            if (longNodes)
            {
                var item = BinUtils.ReadStruct<LSFNodeEntryV3>(reader);
                resolved.ParentIndex = item.ParentIndex;
                resolved.NameIndex = item.NameIndex;
                resolved.NameOffset = item.NameOffset;
                resolved.FirstAttributeIndex = item.FirstAttributeIndex;
            }
            else
            {
                var item = BinUtils.ReadStruct<LSFNodeEntryV2>(reader);
                resolved.ParentIndex = item.ParentIndex;
                resolved.NameIndex = item.NameIndex;
                resolved.NameOffset = item.NameOffset;
                resolved.FirstAttributeIndex = item.FirstAttributeIndex;
            }

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format(
                        "{0}: {1} @ {2:X} (parent {3}, firstAttribute {4})",
                        index, Names[resolved.NameIndex][resolved.NameOffset], pos, resolved.ParentIndex,
                        resolved.FirstAttributeIndex
                    ));
                    index++;
#endif

            _nodeInfos.Add(resolved);
        }
    }

    /// <summary>
    ///     Reads the V2 attribute headers for the LSOF resource
    /// </summary>
    /// <param name="s">Stream to read the attribute headers from</param>
    private void ReadAttributesV2(Stream s)
    {
        using var reader = new BinaryReader(s);
#if DEBUG_LSF_SERIALIZATION
                var rawAttributes = new List<LSFAttributeEntryV2>();
#endif

        var prevAttributeRefs = new List<int>();
        uint dataOffset = 0;
        var index = 0;
        while (s.Position < s.Length)
        {
            var attribute = BinUtils.ReadStruct<LSFAttributeEntryV2>(reader);

            var resolved = new LSFAttributeInfo
            {
                NameIndex = attribute.NameIndex,
                NameOffset = attribute.NameOffset,
                TypeId = attribute.TypeId,
                Length = attribute.Length,
                DataOffset = dataOffset,
                NextAttributeIndex = -1
            };

            var nodeIndex = attribute.NodeIndex + 1;
            if (prevAttributeRefs.Count > nodeIndex)
            {
                if (prevAttributeRefs[nodeIndex] != -1)
                {
                    _attributes[prevAttributeRefs[nodeIndex]].NextAttributeIndex = index;
                }

                prevAttributeRefs[nodeIndex] = index;
            }
            else
            {
                while (prevAttributeRefs.Count < nodeIndex)
                {
                    prevAttributeRefs.Add(-1);
                }

                prevAttributeRefs.Add(index);
            }

#if DEBUG_LSF_SERIALIZATION
                    rawAttributes.Add(attribute);
#endif

            dataOffset += resolved.Length;
            _attributes.Add(resolved);
            index++;
        }

#if DEBUG_LSF_SERIALIZATION
                Console.WriteLine(" ----- DUMP OF ATTRIBUTE REFERENCES -----");
                for (int i = 0; i < prevAttributeRefs.Count; i++)
                {
                    Console.WriteLine(String.Format("Node {0}: last attribute {1}", i, prevAttributeRefs[i]));
                }


                Console.WriteLine(" ----- DUMP OF V2 ATTRIBUTE TABLE -----");
                for (int i = 0; i < Attributes.Count; i++)
                {
                    var resolved = Attributes[i];
                    var attribute = rawAttributes[i];

                    var debug = String.Format(
                        "{0}: {1} (offset {2:X}, typeId {3}, nextAttribute {4}, node {5})",
                        i, Names[resolved.NameIndex][resolved.NameOffset], resolved.DataOffset,
                        resolved.TypeId, resolved.NextAttributeIndex, attribute.NodeIndex
                    );
                    Console.WriteLine(debug);
                }
#endif
    }

    /// <summary>
    ///     Reads the V3 attribute headers for the LSOF resource
    /// </summary>
    /// <param name="s">Stream to read the attribute headers from</param>
    private void ReadAttributesV3(Stream s)
    {
        using var reader = new BinaryReader(s);
        while (s.Position < s.Length)
        {
            var attribute = BinUtils.ReadStruct<LSFAttributeEntryV3>(reader);

            var resolved = new LSFAttributeInfo
            {
                NameIndex = attribute.NameIndex,
                NameOffset = attribute.NameOffset,
                TypeId = attribute.TypeId,
                Length = attribute.Length,
                DataOffset = attribute.Offset,
                NextAttributeIndex = attribute.NextAttributeIndex
            };

            _attributes.Add(resolved);
        }

#if DEBUG_LSF_SERIALIZATION
                Console.WriteLine(" ----- DUMP OF V3 ATTRIBUTE TABLE -----");
                for (int i = 0; i < Attributes.Count; i++)
                {
                    var resolved = Attributes[i];

                    var debug = String.Format(
                        "{0}: {1} (offset {2:X}, typeId {3}, nextAttribute {4})",
                        i, Names[resolved.NameIndex][resolved.NameOffset], resolved.DataOffset,
                        resolved.TypeId, resolved.NextAttributeIndex
                    );
                    Console.WriteLine(debug);
                }
#endif
    }

    private MemoryStream Decompress(
        BinaryReader reader,
        uint sizeOnDisk,
        uint uncompressedSize,
        // ReSharper disable once UnusedParameter.Local
#pragma warning disable S1172
        string debugDumpTo,
#pragma warning restore S1172
        bool allowChunked)
    {
        switch (sizeOnDisk)
        {
            // data is not compressed
            case 0 when uncompressedSize != 0:
            {
                var buf = reader.ReadBytes((int)uncompressedSize);

#if DUMP_LSF_SERIALIZATION
                using (var nodesFile = new FileStream(debugDumpTo, FileMode.Create, FileAccess.Write))
                {
                    nodesFile.Write(buf, 0, buf.Length);
                }
#endif

                return new MemoryStream(buf);
            }

            // no data
            case 0: return new MemoryStream();
        }

        var chunked = _version >= LSFVersion.VerChunkedCompress && allowChunked;
        var isCompressed = BinUtils.CompressionFlagsToMethod(_metadata.CompressionFlags) != CompressionMethod.None;
        var compressedSize = isCompressed
            ? sizeOnDisk
            : uncompressedSize;

        var compressed = reader.ReadBytes((int)compressedSize);
        var uncompressed = BinUtils.Decompress(compressed, (int)uncompressedSize, _metadata.CompressionFlags, chunked);

#if DUMP_LSF_SERIALIZATION
            using (var nodesFile = new FileStream(debugDumpTo, FileMode.Create, FileAccess.Write))
            {
                nodesFile.Write(uncompressed, 0, uncompressed.Length);
            }
#endif

        return new MemoryStream(uncompressed);
    }

    private void ReadHeaders(BinaryReader reader)
    {
        var magic = BinUtils.ReadStruct<LSFMagic>(reader);
        if (magic.Magic != BitConverter.ToUInt32(LSFMagic.Signature, 0))
        {
            var msg = $"Invalid LSF signature; expected {BitConverter.ToUInt32(LSFMagic.Signature, 0),8:X}, got {magic.Magic,8:X}";
            throw new InvalidDataException(msg);
        }

        if (magic.Version < (ulong)LSFVersion.VerInitial || magic.Version > (ulong)LSFVersion.MaxReadVersion)
        {
            var msg = $"LSF version {magic.Version} is not supported";
            throw new InvalidDataException(msg);
        }

        _version = (LSFVersion)magic.Version;

        if (_version >= LSFVersion.VerBG3ExtendedHeader)
        {
            var hdr = BinUtils.ReadStruct<LSFHeaderV5>(reader);
            _gameVersion = PackedVersion.FromInt64(hdr.EngineVersion);

            // Workaround for merged LSF files with missing engine version number
            if (_gameVersion.Major == 0)
            {
                _gameVersion.Major = 4;
                _gameVersion.Minor = 0;
                _gameVersion.Revision = 9;
                _gameVersion.Build = 0;
            }
        }
        else
        {
            var hdr = BinUtils.ReadStruct<LSFHeader>(reader);
            _gameVersion = PackedVersion.FromInt32(hdr.EngineVersion);
        }

        if (_version < LSFVersion.VerBG3AdditionalBlob)
        {
            var meta = BinUtils.ReadStruct<LSFMetadataV5>(reader);
            _metadata = new LSFMetadataV6
            {
                StringsUncompressedSize = meta.StringsUncompressedSize,
                StringsSizeOnDisk = meta.StringsSizeOnDisk,
                NodesUncompressedSize = meta.NodesUncompressedSize,
                NodesSizeOnDisk = meta.NodesSizeOnDisk,
                AttributesUncompressedSize = meta.AttributesUncompressedSize,
                AttributesSizeOnDisk = meta.AttributesSizeOnDisk,
                ValuesUncompressedSize = meta.ValuesUncompressedSize,
                ValuesSizeOnDisk = meta.ValuesSizeOnDisk,
                CompressionFlags = meta.CompressionFlags,
                HasSiblingData = meta.HasSiblingData
            };
        }
        else
        {
            _metadata = BinUtils.ReadStruct<LSFMetadataV6>(reader);
        }
    }

    private void ReadRegions(Resource? resource)
    {
        var attrReader = new BinaryReader(_values);
        _nodeInstances = new List<Node?>();
        foreach (var nodeInfo in _nodeInfos)
        {
            if (nodeInfo.ParentIndex == -1)
            {
                var region = new Region();
                ReadNode(nodeInfo, region, attrReader);
                _nodeInstances.Add(region);
                region.RegionName = region.Name;
                resource.Regions[region.Name] = region;
            }
            else
            {
                var node = new Node();
                ReadNode(nodeInfo, node, attrReader);
                node.Parent = _nodeInstances[nodeInfo.ParentIndex];
                _nodeInstances.Add(node);
                _nodeInstances[nodeInfo.ParentIndex].AppendChild(node);
            }
        }
    }

    private void ReadNode(LSFNodeInfo nodeInfo, Node? node, BinaryReader attributeReader)
    {
        node.Name = _names[nodeInfo.NameIndex][nodeInfo.NameOffset];

#if DEBUG_LSF_SERIALIZATION
            Console.WriteLine(String.Format("Begin node {0}", node.Name));
#endif

        if (nodeInfo.FirstAttributeIndex == -1)
        {
            return;
        }

        var attribute = _attributes[nodeInfo.FirstAttributeIndex];
        while (true)
        {
            _values.Position = attribute.DataOffset;
            var value = ReadAttribute((NodeAttribute.DataType)attribute.TypeId, attributeReader, attribute.Length);
            node.Attributes[_names[attribute.NameIndex][attribute.NameOffset]] = value;

#if DEBUG_LSF_SERIALIZATION
                    Console.WriteLine(String.Format("    {0:X}: {1} ({2})", attribute.DataOffset, Names[attribute.NameIndex][attribute.NameOffset], value));
#endif

            if (attribute.NextAttributeIndex == -1)
            {
                break;
            }

            attribute = _attributes[attribute.NextAttributeIndex];
        }
    }

    private NodeAttribute ReadAttribute(NodeAttribute.DataType type, BinaryReader reader, uint length)
    {
        // LSF and LSB serialize the buffer types differently, so specialized
        // code is added to the LSB and LSf serializers, and the common code is
        // available in BinUtils.ReadAttribute()
        switch (type)
        {
            case NodeAttribute.DataType.DT_String:
            case NodeAttribute.DataType.DT_Path:
            case NodeAttribute.DataType.DT_FixedString:
            case NodeAttribute.DataType.DT_LSString:
            case NodeAttribute.DataType.DT_WString:
            case NodeAttribute.DataType.DT_LSWString:
            {
                var attr = new NodeAttribute(type)
                {
                    Value = ReadString(reader, (int)length)
                };

                return attr;
            }

            case NodeAttribute.DataType.DT_TranslatedString:
            {
                var attr = new NodeAttribute(type);
                var str = new TranslatedString();

                if (_version >= LSFVersion.VerBG3
                 || _gameVersion.Major > 4
                 || _gameVersion is { Major: 4, Revision: > 0 }
                 || _gameVersion.Major == 4 && _gameVersion is { Revision: 0, Build: >= 0x1a })
                {
                    str.Version = reader.ReadUInt16();
                }
                else
                {
                    str.Version = 0;
                    var valueLength = reader.ReadInt32();
                    str.Value = ReadString(reader, valueLength);
                }

                var handleLength = reader.ReadInt32();
                str.Handle = ReadString(reader, handleLength);

                attr.Value = str;
                return attr;
            }

            case NodeAttribute.DataType.DT_TranslatedFSString:
            {
                var attr = new NodeAttribute(type)
                {
                    Value = ReadTranslatedFSString(reader)
                };

                return attr;
            }

            case NodeAttribute.DataType.DT_ScratchBuffer:
            {
                var attr = new NodeAttribute(type)
                {
                    Value = reader.ReadBytes((int)length)
                };

                return attr;
            }

            default:
                return BinUtils.ReadAttribute(type, reader);
        }
    }

    private TranslatedFSString ReadTranslatedFSString(BinaryReader reader)
    {
        var str = new TranslatedFSString();

        if (_version >= LSFVersion.VerBG3)
        {
            str.Version = reader.ReadUInt16();
        }
        else
        {
            str.Version = 0;
            var valueLength = reader.ReadInt32();
            str.Value = ReadString(reader, valueLength);
        }

        var handleLength = reader.ReadInt32();
        str.Handle = ReadString(reader, handleLength);

        var arguments = reader.ReadInt32();
        str.Arguments = new List<TranslatedFSStringArgument>(arguments);
        for (var i = 0; i < arguments; i++)
        {
            var arg = new TranslatedFSStringArgument();
            var argKeyLength = reader.ReadInt32();
            arg.Key = ReadString(reader, argKeyLength);

            arg.String = ReadTranslatedFSString(reader);

            var argValueLength = reader.ReadInt32();
            arg.Value = ReadString(reader, argValueLength);

            str.Arguments.Add(arg);
        }

        return str;
    }

    private static string ReadString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length - 1);

        // Remove null bytes at the end of the string
        var lastNull = bytes.Length;
        while (lastNull > 0 && bytes[lastNull - 1] == 0)
        {
            lastNull--;
        }

        var nullTerminator = reader.ReadByte();
        if (nullTerminator != 0)
        {
            throw new InvalidDataException("String is not null-terminated");
        }

        return Encoding.UTF8.GetString(bytes, 0, lastNull);
    }
}