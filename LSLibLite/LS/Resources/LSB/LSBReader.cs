using System.Text;

namespace LSLibLite.LS.Resources.LSB;

public sealed class LSBReader : ILSReader
{
    #region Members

    private BinaryReader _reader;
    private bool _isBG3;
    private readonly Dictionary<uint, string?> _staticStrings = new();
    private readonly Stream? _stream;

    #endregion

    #region Constructors

    public LSBReader(Stream? stream)
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
        using (_reader = new BinaryReader(_stream))
        {
            // Check for BG3 header
            var header = BinUtils.ReadStruct<LSBHeader>(_reader);
            if (header.Signature != BitConverter.ToUInt32(LSBHeader.SignatureBG3.AsSpan()) && header.Signature != LSBHeader.SignatureFW3)
            {
                throw new InvalidFormatException($"Illegal signature in LSB header ({header.Signature})");
            }

            if (_stream.Length != header.TotalSize)
            {
                throw new InvalidFormatException($"Invalid LSB file size; expected {header.TotalSize}, got {_stream.Length}");
            }

            // The game only uses little-endian files on all platforms currently and big-endian support isn't worth the hassle
            if (header.BigEndian != 0)
            {
                throw new InvalidFormatException("Big-endian LSB files are not supported");
            }

            _isBG3 = header.Signature == BitConverter.ToUInt32(LSBHeader.SignatureBG3.AsSpan());
            ReadStaticStrings();

            Resource resource = new()
            {
                Metadata = header.Metadata
            };

            ReadRegions(resource);
            return resource;
        }
    }

    private void ReadRegions(Resource? resource)
    {
        var regions = _reader.ReadUInt32();
        for (uint i = 0; i < regions; i++)
        {
            var regionNameId = _reader.ReadUInt32();
            var regionOffset = _reader.ReadUInt32();

            Region rgn = new()
            {
                RegionName = _staticStrings[regionNameId]
            };

            var lastRegionPos = _stream.Position;

            _stream.Seek(regionOffset, SeekOrigin.Begin);
            ReadNode(rgn);
            resource.Regions[rgn.RegionName] = rgn;
            _stream.Seek(lastRegionPos, SeekOrigin.Begin);
        }
    }

    private void ReadNode(Node? node)
    {
        var nodeNameId = _reader.ReadUInt32();
        var attributeCount = _reader.ReadUInt32();
        var childCount = _reader.ReadUInt32();
        node.Name = _staticStrings[nodeNameId];

        for (uint i = 0; i < attributeCount; i++)
        {
            var attrNameId = _reader.ReadUInt32();
            var attrTypeId = _reader.ReadUInt32();
            if (attrTypeId > (int)NodeAttribute.DataType.DT_Max)
            {
                throw new InvalidFormatException($"Unsupported attribute data type: {attrTypeId}");
            }

            node.Attributes[_staticStrings[attrNameId]] = ReadAttribute((NodeAttribute.DataType)attrTypeId);
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

                if (_isBG3)
                {
                    str.Version = _reader.ReadUInt16();

                    // Sometimes BG3 string keys still contain the value?
                    // Weird heuristic to find these cases
                    var test = _reader.ReadUInt16();
                    if (test == 0)
                    {
                        _stream.Seek(-4, SeekOrigin.Current);
                        str.Version = 0;
                        str.Value = ReadString(true);
                    }
                    else
                    {
                        _stream.Seek(-2, SeekOrigin.Current);
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
                var bufferLength = _reader.ReadInt32();

                return new NodeAttribute(type)
                {
                    Value = _reader.ReadBytes(bufferLength)
                };
            }

            // DT_TranslatedFSString not supported in LSB
            default:
                return BinUtils.ReadAttribute(type, _reader);
        }
    }

    private void ReadStaticStrings()
    {
        var strings = _reader.ReadUInt32();
        for (uint i = 0; i < strings; i++)
        {
            var s = ReadString(false);
            var index = _reader.ReadUInt32();
            if (_staticStrings.ContainsKey(index))
            {
                throw new InvalidFormatException($"String ID {index} duplicated in static string map");
            }

            _staticStrings.Add(index, s);
        }
    }

    private string ReadString(bool nullTerminated)
    {
        var length = _reader.ReadInt32()
                   - (nullTerminated
                         ? 1
                         : 0);

        var bytes = _reader.ReadBytes(length);

        // Remove stray null bytes at the end of the string
        // Some LSB files seem to save translated string keys incorrectly, and append two NULL bytes
        // (or one null byte and another stray byte) to the end of the value.
        var hasBogusNullBytes = false;
        while (length > 0 && bytes[length - 1] == 0)
        {
            length--;
            hasBogusNullBytes = true;
        }

        var str = Encoding.UTF8.GetString(bytes, 0, length);

        if (!nullTerminated)
        {
            return str;
        }

        return _reader.ReadByte() == 0 || hasBogusNullBytes
            ? str
            : throw new InvalidFormatException("Illegal null terminated string");
    }

    private string ReadWideString(bool nullTerminated)
    {
        var length = _reader.ReadInt32()
                   - (nullTerminated
                         ? 1
                         : 0);

        var bytes = _reader.ReadBytes(length * 2);
        var str = Encoding.Unicode.GetString(bytes);
        if (!nullTerminated)
        {
            return str;
        }

        return _reader.ReadUInt16() == 0
            ? str
            : throw new InvalidFormatException("Illegal null terminated widestring");
    }
}