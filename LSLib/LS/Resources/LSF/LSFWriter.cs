﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LSLib.LS.Enums;

namespace LSLib.LS;

public class LSFWriter :ILSWriter
{
    private static int StringHashMapSize = 0x200;

    private Stream Stream;
    private BinaryWriter Writer;
    private LSMetadata Meta;

    private MemoryStream NodeStream;
    private BinaryWriter NodeWriter;
    private int NextNodeIndex = 0;
    private Dictionary<Node, int> NodeIndices;

    private MemoryStream AttributeStream;
    private BinaryWriter AttributeWriter;
    private int NextAttributeIndex = 0;

    private MemoryStream ValueStream;
    private BinaryWriter ValueWriter;

    private List<List<string>> StringHashMap;
    private List<int> NextSiblingIndices;

    public LSFVersion Version = LSFVersion.MaxWriteVersion;
    public bool EncodeSiblingData = false;
    public CompressionMethod Compression = CompressionMethod.LZ4;
    public CompressionLevel CompressionLevel = CompressionLevel.DefaultCompression;

    public LSFWriter(Stream stream)
    {
        Stream = stream;
    }

    public void Write(Resource resource)
    {
        if (Version > LSFVersion.MaxWriteVersion)
        {
            var msg = string.Format("Writing LSF version {0} is not supported (highest is {1})", Version, LSFVersion.MaxWriteVersion);
            throw new InvalidDataException(msg);
        }

        Meta = resource.Metadata;

        using (Writer = new(Stream, Encoding.Default, true))
        using (NodeStream = new())
        using (NodeWriter = new(NodeStream))
        using (AttributeStream = new())
        using (AttributeWriter = new(AttributeStream))
        using (ValueStream = new())
        using (ValueWriter = new(ValueStream))
        {
            NextNodeIndex = 0;
            NextAttributeIndex = 0;
            NodeIndices = new();
            NextSiblingIndices = null;
            StringHashMap = new(StringHashMapSize);
            while (StringHashMap.Count < StringHashMapSize)
            {
                StringHashMap.Add(new());
            }

            if (EncodeSiblingData)
            {
                ComputeSiblingIndices(resource);
            }

            WriteRegions(resource);

            byte[] stringBuffer = null;
            using (var stringStream = new MemoryStream())
            using (var stringWriter = new BinaryWriter(stringStream))
            {
                WriteStaticStrings(stringWriter);
                stringBuffer = stringStream.ToArray();
            }

            var nodeBuffer = NodeStream.ToArray();
            var attributeBuffer = AttributeStream.ToArray();
            var valueBuffer = ValueStream.ToArray();

            var magic = new LSFMagic
            {
                Magic = BitConverter.ToUInt32(LSFMagic.Signature, 0),
                Version = (uint)Version
            };

            BinUtils.WriteStruct<LSFMagic>(Writer, ref magic);

            PackedVersion gameVersion = new()
            {
                Major = resource.Metadata.MajorVersion,
                Minor = resource.Metadata.MinorVersion,
                Revision = resource.Metadata.Revision,
                Build = resource.Metadata.BuildNumber
            };

            if (Version < LSFVersion.VerBG3ExtendedHeader)
            {
                var header = new LSFHeader
                {
                    EngineVersion = gameVersion.ToVersion32()
                };

                BinUtils.WriteStruct<LSFHeader>(Writer, ref header);
            }
            else
            {
                var header = new LSFHeaderV5
                {
                    EngineVersion = gameVersion.ToVersion64()
                };

                BinUtils.WriteStruct<LSFHeaderV5>(Writer, ref header);
            }

            bool chunked = Version >= LSFVersion.VerChunkedCompress;
            byte[] stringsCompressed = BinUtils.Compress(stringBuffer, Compression, CompressionLevel);
            byte[] nodesCompressed = BinUtils.Compress(nodeBuffer, Compression, CompressionLevel, chunked);
            byte[] attributesCompressed = BinUtils.Compress(attributeBuffer, Compression, CompressionLevel, chunked);
            byte[] valuesCompressed = BinUtils.Compress(valueBuffer, Compression, CompressionLevel, chunked);

            if (Version < LSFVersion.VerBG3AdditionalBlob)
            {
                var meta = new LSFMetadataV5
                {
                    StringsUncompressedSize = (uint)stringBuffer.Length,
                    NodesUncompressedSize = (uint)nodeBuffer.Length,
                    AttributesUncompressedSize = (uint)attributeBuffer.Length,
                    ValuesUncompressedSize = (uint)valueBuffer.Length
                };

                if (Compression == CompressionMethod.None)
                {
                    meta.StringsSizeOnDisk = 0;
                    meta.NodesSizeOnDisk = 0;
                    meta.AttributesSizeOnDisk = 0;
                    meta.ValuesSizeOnDisk = 0;
                }
                else
                {
                    meta.StringsSizeOnDisk = (uint)stringsCompressed.Length;
                    meta.NodesSizeOnDisk = (uint)nodesCompressed.Length;
                    meta.AttributesSizeOnDisk = (uint)attributesCompressed.Length;
                    meta.ValuesSizeOnDisk = (uint)valuesCompressed.Length;
                }

                meta.CompressionFlags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel);
                meta.Unknown2 = 0;
                meta.Unknown3 = 0;
                meta.HasSiblingData = EncodeSiblingData ? 1u : 0u;

                BinUtils.WriteStruct<LSFMetadataV5>(Writer, ref meta);
            }
            else
            {
                var meta = new LSFMetadataV6
                {
                    StringsUncompressedSize = (uint)stringBuffer.Length,
                    NodesUncompressedSize = (uint)nodeBuffer.Length,
                    AttributesUncompressedSize = (uint)attributeBuffer.Length,
                    ValuesUncompressedSize = (uint)valueBuffer.Length
                };

                if (Compression == CompressionMethod.None)
                {
                    meta.StringsSizeOnDisk = 0;
                    meta.NodesSizeOnDisk = 0;
                    meta.AttributesSizeOnDisk = 0;
                    meta.ValuesSizeOnDisk = 0;
                }
                else
                {
                    meta.StringsSizeOnDisk = (uint)stringsCompressed.Length;
                    meta.NodesSizeOnDisk = (uint)nodesCompressed.Length;
                    meta.AttributesSizeOnDisk = (uint)attributesCompressed.Length;
                    meta.ValuesSizeOnDisk = (uint)valuesCompressed.Length;
                }

                meta.Unknown = 0;
                meta.CompressionFlags = BinUtils.MakeCompressionFlags(Compression, CompressionLevel);
                meta.Unknown2 = 0;
                meta.Unknown3 = 0;
                meta.HasSiblingData = EncodeSiblingData ? 1u : 0u;

                BinUtils.WriteStruct<LSFMetadataV6>(Writer, ref meta);
            }

            Writer.Write(stringsCompressed, 0, stringsCompressed.Length);
            Writer.Write(nodesCompressed, 0, nodesCompressed.Length);
            Writer.Write(attributesCompressed, 0, attributesCompressed.Length);
            Writer.Write(valuesCompressed, 0, valuesCompressed.Length);
        }
    }

    private int ComputeSiblingIndices(Node node)
    {
        int index = NextNodeIndex;
        NextNodeIndex++;
        NextSiblingIndices.Add(-1);

        int lastSiblingIndex = -1;
        foreach (var children in node.Children)
        {
            foreach (var child in children.Value)
            {
                int childIndex = ComputeSiblingIndices(child);
                if (lastSiblingIndex != -1)
                {
                    NextSiblingIndices[lastSiblingIndex] = childIndex;
                }

                lastSiblingIndex = childIndex;
            }
        }

        return index;
    }

    private void ComputeSiblingIndices(Resource resource)
    {
        NextNodeIndex = 0;
        NextSiblingIndices = new();

        int lastRegionIndex = -1;
        foreach (var region in resource.Regions)
        {
            int regionIndex = ComputeSiblingIndices(region.Value);
            if (lastRegionIndex != -1)
            {
                NextSiblingIndices[lastRegionIndex] = regionIndex;
            }

            lastRegionIndex = regionIndex;
        }
    }

    private void WriteRegions(Resource resource)
    {
        NextNodeIndex = 0;
        foreach (var region in resource.Regions)
        {
            if (Version >= LSFVersion.VerExtendedNodes
             && EncodeSiblingData)
            {
                WriteNodeV3(region.Value);
            }
            else
            {
                WriteNodeV2(region.Value);
            }
        }
    }

    private void WriteNodeAttributesV2(Node node)
    {
        uint lastOffset = (uint)ValueStream.Position;
        foreach (KeyValuePair<string, NodeAttribute> entry in node.Attributes)
        {
            WriteAttributeValue(ValueWriter, entry.Value);

            var attributeInfo = new LSFAttributeEntryV2();
            var length = (uint)ValueStream.Position - lastOffset;
            attributeInfo.TypeAndLength = (uint)entry.Value.Type | (length << 6);
            attributeInfo.NameHashTableIndex = AddStaticString(entry.Key);
            attributeInfo.NodeIndex = NextNodeIndex;
            BinUtils.WriteStruct<LSFAttributeEntryV2>(AttributeWriter, ref attributeInfo);
            NextAttributeIndex++;

            lastOffset = (uint)ValueStream.Position;
        }
    }

    private void WriteNodeAttributesV3(Node node)
    {
        uint lastOffset = (uint)ValueStream.Position;
        int numWritten = 0;
        foreach (KeyValuePair<string, NodeAttribute> entry in node.Attributes)
        {
            WriteAttributeValue(ValueWriter, entry.Value);
            numWritten++;

            var attributeInfo = new LSFAttributeEntryV3();
            var length = (uint)ValueStream.Position - lastOffset;
            attributeInfo.TypeAndLength = (uint)entry.Value.Type | (length << 6);
            attributeInfo.NameHashTableIndex = AddStaticString(entry.Key);
            if (numWritten == node.Attributes.Count)
            {
                attributeInfo.NextAttributeIndex = -1;
            }
            else
            {
                attributeInfo.NextAttributeIndex = NextAttributeIndex + 1;
            }
            attributeInfo.Offset = lastOffset;
            BinUtils.WriteStruct<LSFAttributeEntryV3>(AttributeWriter, ref attributeInfo);

            NextAttributeIndex++;

            lastOffset = (uint)ValueStream.Position;
        }
    }

    private void WriteNodeChildren(Node node)
    {
        foreach (var children in node.Children)
        {
            foreach (var child in children.Value)
            {
                if (Version >= LSFVersion.VerExtendedNodes && EncodeSiblingData)
                {
                    WriteNodeV3(child);
                }
                else
                {
                    WriteNodeV2(child);
                }
            }
        }
    }

    private void WriteNodeV2(Node node)
    {
        var nodeInfo = new LSFNodeEntryV2();
        if (node.Parent == null)
        {
            nodeInfo.ParentIndex = -1;
        }
        else
        {
            nodeInfo.ParentIndex = NodeIndices[node.Parent];
        }

        nodeInfo.NameHashTableIndex = AddStaticString(node.Name);

        if (node.Attributes.Count > 0)
        {
            nodeInfo.FirstAttributeIndex = NextAttributeIndex;
            WriteNodeAttributesV2(node);
        }
        else
        {
            nodeInfo.FirstAttributeIndex = -1;
        }

        BinUtils.WriteStruct<LSFNodeEntryV2>(NodeWriter, ref nodeInfo);
        NodeIndices[node] = NextNodeIndex;
        NextNodeIndex++;

        WriteNodeChildren(node);
    }

    private void WriteNodeV3(Node node)
    {
        var nodeInfo = new LSFNodeEntryV3();
        if (node.Parent == null)
        {
            nodeInfo.ParentIndex = -1;
        }
        else
        {
            nodeInfo.ParentIndex = NodeIndices[node.Parent];
        }

        nodeInfo.NameHashTableIndex = AddStaticString(node.Name);

        // Assumes we calculated indices first using ComputeSiblingIndices()
        nodeInfo.NextSiblingIndex = NextSiblingIndices[NextNodeIndex];

        if (node.Attributes.Count > 0)
        {
            nodeInfo.FirstAttributeIndex = NextAttributeIndex;
            WriteNodeAttributesV3(node);
        }
        else
        {
            nodeInfo.FirstAttributeIndex = -1;
        }

        BinUtils.WriteStruct<LSFNodeEntryV3>(NodeWriter, ref nodeInfo);
        NodeIndices[node] = NextNodeIndex;
        NextNodeIndex++;

        WriteNodeChildren(node);
    }

    private void WriteTranslatedFSString(BinaryWriter writer, TranslatedFSString fs)
    {
        if (Version >= LSFVersion.VerBG3 ||
            Meta.MajorVersion > 4 ||
            (Meta.MajorVersion == 4 && Meta.Revision > 0) ||
            (Meta.MajorVersion == 4 && Meta.Revision == 0 && Meta.BuildNumber >= 0x1a))
        {
            writer.Write(fs.Version);
        }
        else
        {
            WriteStringWithLength(writer, fs.Value ?? "");
        }

        WriteStringWithLength(writer, fs.Handle);

        writer.Write((uint)fs.Arguments.Count);
        foreach (var arg in fs.Arguments)
        {
            WriteStringWithLength(writer, arg.Key);
            WriteTranslatedFSString(writer, arg.String);
            WriteStringWithLength(writer, arg.Value);
        }
    }

    private void WriteAttributeValue(BinaryWriter writer, NodeAttribute attr)
    {
        switch (attr.Type)
        {
            case NodeAttribute.DataType.DT_String:
            case NodeAttribute.DataType.DT_Path:
            case NodeAttribute.DataType.DT_FixedString:
            case NodeAttribute.DataType.DT_LSString:
            case NodeAttribute.DataType.DT_WString:
            case NodeAttribute.DataType.DT_LSWString:
                WriteString(writer, (string)attr.Value);
                break;

            case NodeAttribute.DataType.DT_TranslatedString:
            {
                var ts = (TranslatedString)attr.Value;
                if (Version >= LSFVersion.VerBG3)
                {
                    writer.Write(ts.Version);
                }
                else
                {
                    WriteStringWithLength(writer, ts.Value ?? "");
                }

                WriteStringWithLength(writer, ts.Handle);
                break;
            }

            case NodeAttribute.DataType.DT_TranslatedFSString:
            {
                var fs = (TranslatedFSString)attr.Value;
                WriteTranslatedFSString(writer, fs);
                break;
            }

            case NodeAttribute.DataType.DT_ScratchBuffer:
            {
                var buffer = (byte[])attr.Value;
                writer.Write(buffer);
                break;
            }

            default:
                BinUtils.WriteAttribute(writer, attr);
                break;
        }
    }

    private uint AddStaticString(string s)
    {
        var hashCode = (uint)s.GetHashCode();
        var bucket = (int)((hashCode & 0x1ff) ^ ((hashCode >> 9) & 0x1ff) ^ ((hashCode >> 18) & 0x1ff) ^ ((hashCode >> 27) & 0x1ff));
        for (int i = 0; i < StringHashMap[bucket].Count; i++)
        {
            if (StringHashMap[bucket][i].Equals(s))
            {
                return (uint)((bucket << 16) | i);
            }
        }

        StringHashMap[bucket].Add(s);
        return (uint)((bucket << 16) | (StringHashMap[bucket].Count - 1));
    }

    private void WriteStaticStrings(BinaryWriter writer)
    {
        writer.Write((uint)StringHashMap.Count);
        for (int i = 0; i < StringHashMap.Count; i++)
        {
            var entry = StringHashMap[i];
            writer.Write((ushort)entry.Count);
            for (int j = 0; j < entry.Count; j++)
            {
                WriteStaticString(writer, entry[j]);
            }
        }
    }

    private void WriteStaticString(BinaryWriter writer, string s)
    {
        byte[] utf = Encoding.UTF8.GetBytes(s);
        writer.Write((ushort)utf.Length);
        writer.Write(utf);
    }

    private void WriteStringWithLength(BinaryWriter writer, string s)
    {
        byte[] utf = Encoding.UTF8.GetBytes(s);
        writer.Write((int)(utf.Length + 1));
        writer.Write(utf);
        writer.Write((byte)0);
    }

    private void WriteString(BinaryWriter writer, string s)
    {
        byte[] utf = Encoding.UTF8.GetBytes(s);
        writer.Write(utf);
        writer.Write((byte)0);
    }
}