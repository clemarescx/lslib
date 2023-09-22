using System;
using System.Collections.Generic;
using System.IO;

namespace LSLib.LS;

public sealed class LSBReader : ILSReader
{
    private Stream stream;
    private BinaryReader reader;
    private Dictionary<uint, string> staticStrings = new();
    private bool IsBG3;

    public LSBReader(Stream stream)
    {
        this.stream = stream;
    }

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource Read()
    {
        using (reader = new BinaryReader(stream))
        {
            // Check for BG3 header
            var header = BinUtils.ReadStruct<LSBHeader>(reader);
            if (header.Signature != BitConverter.ToUInt32(LSBHeader.SignatureBG3.AsSpan()) && header.Signature != LSBHeader.SignatureFW3)
            {
                throw new InvalidFormatException($"Illegal signature in LSB header ({header.Signature})");
            }

            if (stream.Length != header.TotalSize)
            {
                throw new InvalidFormatException($"Invalid LSB file size; expected {header.TotalSize}, got {stream.Length}");
            }

            // The game only uses little-endian files on all platforms currently and big-endian support isn't worth the hassle
            if (header.BigEndian != 0)
            {
                throw new InvalidFormatException("Big-endian LSB files are not supported");
            }

            IsBG3 = header.Signature == BitConverter.ToUInt32(LSBHeader.SignatureBG3.AsSpan());
            ReadStaticStrings();

            Resource resource = new()
            {
                Metadata = header.Metadata
            };

            ReadRegions(resource);
            return resource;
        }
    }

    private void ReadRegions(Resource resource)
    {
        var regions = reader.ReadUInt32();
        for (uint i = 0; i < regions; i++)
        {
            var regionNameId = reader.ReadUInt32();
            var regionOffset = reader.ReadUInt32();

            Region rgn = new()
            {
                RegionName = staticStrings[regionNameId]
            };

            var lastRegionPos = stream.Position;

            stream.Seek(regionOffset, SeekOrigin.Begin);
            ReadNode(rgn);
            resource.Regions[rgn.RegionName] = rgn;
            stream.Seek(lastRegionPos, SeekOrigin.Begin);
        }
    }

    private void ReadNode(Node node)
    {
        var nodeNameId = reader.ReadUInt32();
        var attributeCount = reader.ReadUInt32();
        var childCount = reader.ReadUInt32();
        node.Name = staticStrings[nodeNameId];

        for (uint i = 0; i < attributeCount; i++)
        {
            var attrNameId = reader.ReadUInt32();
            var attrTypeId = reader.ReadUInt32();
            if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
            {
                throw new InvalidFormatException($"Unsupported attribute data type: {attrTypeId}");
            }

            node.Attributes[staticStrings[attrNameId]] = ReadAttribute((NodeAttribute.DataType)attrTypeId);
        }

        for (uint i = 0; i < childCount; i++)
        {
            Node child = new()
            {
                Parent = node
            };

            ReadNode(child);
            node.AppendChild(child);
        }
    }

    private NodeAttribute ReadAttribute(NodeAttribute.DataType type)
    {
        switch (type)
        {
            case NodeAttribute.DataType.DT_String:
            case NodeAttribute.DataType.DT_Path:
            case NodeAttribute.DataType.DT_FixedString:
            case NodeAttribute.DataType.DT_LSString:
            {
                return new NodeAttribute(type)
                {
                    Value = ReadString(true)
                };
            }

            case NodeAttribute.DataType.DT_WString:
            case NodeAttribute.DataType.DT_LSWString:
            {
                return new NodeAttribute(type)
                {
                    Value = ReadWideString(true)
                };
            }

            case NodeAttribute.DataType.DT_TranslatedString:
            {
                var str = new TranslatedString();

                if (IsBG3)
                {
                    str.Version = reader.ReadUInt16();

                    // Sometimes BG3 string keys still contain the value?
                    // Weird heuristic to find these cases
                    var test = reader.ReadUInt16();
                    if (test == 0)
                    {
                        stream.Seek(-4, SeekOrigin.Current);
                        str.Version = 0;
                        str.Value = ReadString(true);
                    }
                    else
                    {
                        stream.Seek(-2, SeekOrigin.Current);
                        str.Value = null;
                    }
                }
                else
                {
                    str.Version = 0;
                    str.Value = ReadString(true);
                }

                str.Handle = ReadString(true);

                return new NodeAttribute(type)
                {
                    Value = str
                };
            }

            case NodeAttribute.DataType.DT_ScratchBuffer:
            {
                var bufferLength = reader.ReadInt32();

                return new NodeAttribute(type)
                {
                    Value = reader.ReadBytes(bufferLength)
                };
            }

            // DT_TranslatedFSString not supported in LSB
            default:
                return BinUtils.ReadAttribute(type, reader);
        }
    }

    private void ReadStaticStrings()
    {
        var strings = reader.ReadUInt32();
        for (uint i = 0; i < strings; i++)
        {
            var s = ReadString(false);
            var index = reader.ReadUInt32();
            if (staticStrings.ContainsKey(index))
            {
                throw new InvalidFormatException($"String ID {index} duplicated in static string map");
            }

            staticStrings.Add(index, s);
        }
    }

    private string ReadString(bool nullTerminated)
    {
        var length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
        var bytes = reader.ReadBytes(length);

        // Remove stray null bytes at the end of the string
        // Some LSB files seem to save translated string keys incurrectly, and append two NULL bytes
        // (or one null byte and another stray byte) to the end of the value.
        var hasBogusNullBytes = false;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
            hasBogusNullBytes = true;
        }

        var str = System.Text.Encoding.UTF8.GetString(bytes, 0, length);

        if (!nullTerminated)
        {
            return str;
        }

        return reader.ReadByte() == 0 || hasBogusNullBytes
            ? str
            : throw new InvalidFormatException("Illegal null terminated string");
    }

    private string ReadWideString(bool nullTerminated)
    {
        var length = reader.ReadInt32() - (nullTerminated ? 1 : 0);
        var bytes = reader.ReadBytes(length * 2);
        var str = System.Text.Encoding.Unicode.GetString(bytes);
        if (!nullTerminated)
        {
            return str;
        }

        return reader.ReadUInt16() == 0
            ? str
            : throw new InvalidFormatException("Illegal null terminated widestring");
    }
}