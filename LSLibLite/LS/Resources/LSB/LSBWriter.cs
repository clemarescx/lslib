using System.Text;

namespace LSLibLite.LS.Resources.LSB;

public class LSBWriter : ILSWriter
{
    #region Members

    private readonly Dictionary<string, uint> staticStrings = new();
    private readonly Stream _stream;
    private BinaryWriter writer;
    private uint nextStaticStringId;
    private uint Version;

    #endregion

    #region Constructors

    public LSBWriter(Stream stream)
    {
        _stream = stream;
    }

    #endregion

    public void Write(Resource? resource)
    {
        Version = resource.Metadata.MajorVersion;
        using (writer = new BinaryWriter(_stream))
        {
            var header = new LSBHeader
            {
                TotalSize = 0, // Total size of file, will be updater after we finished serializing
                BigEndian = 0, // Little-endian format
                Unknown = 0,   // Unknown
                Metadata = resource.Metadata,
                Signature = resource.Metadata.MajorVersion >= 4
                    ? BitConverter.ToUInt32(LSBHeader.SignatureBG3.AsSpan())
                    : LSBHeader.SignatureFW3
            };

            BinUtils.WriteStruct(writer, ref header);

            CollectStaticStrings(resource);
            WriteStaticStrings();

            WriteRegions(resource);

            header.TotalSize = (uint)_stream.Position;
            _stream.Seek(0, SeekOrigin.Begin);
            BinUtils.WriteStruct(writer, ref header);
        }
    }

    private void WriteRegions(Resource? resource)
    {
        writer.Write((uint)resource.Regions.Count);
        var regionMapOffset = _stream.Position;
        foreach (var rgn in resource.Regions)
        {
            writer.Write(staticStrings[rgn.Key]);
            writer.Write((uint)0); // Offset of region, will be updater after we finished serializing
        }

        List<uint> regionPositions = new();
        foreach (var rgn in resource.Regions)
        {
            regionPositions.Add((uint)_stream.Position);
            WriteNode(rgn.Value);
        }

        var endOffset = _stream.Position;
        _stream.Seek(regionMapOffset, SeekOrigin.Begin);
        foreach (var position in regionPositions)
        {
            _stream.Seek(4, SeekOrigin.Current);
            writer.Write(position);
        }

        _stream.Seek(endOffset, SeekOrigin.Begin);
    }

    private void WriteNode(Node? node)
    {
        writer.Write(staticStrings[node.Name]);
        writer.Write((uint)node.Attributes.Count);
        writer.Write((uint)node.ChildCount);

        foreach (var attribute in node.Attributes)
        {
            writer.Write(staticStrings[attribute.Key]);
            writer.Write((uint)attribute.Value.Type);
            WriteAttribute(attribute.Value);
        }

        foreach (var child in node.Children.SelectMany(children => children.Value))
        {
            WriteNode(child);
        }
    }

    private void WriteAttribute(NodeAttribute attr)
    {
        switch (attr.Type)
        {
            case NodeAttribute.DataType.DT_String:
            case NodeAttribute.DataType.DT_Path:
            case NodeAttribute.DataType.DT_FixedString:
            case NodeAttribute.DataType.DT_LSString:
                WriteString((string)attr.Value, true);
                break;

            case NodeAttribute.DataType.DT_WString:
            case NodeAttribute.DataType.DT_LSWString:
                WriteWideString((string)attr.Value, true);
                break;

            case NodeAttribute.DataType.DT_TranslatedString:
            {
                var str = (TranslatedString)attr.Value;
                if (Version >= 4 && str.Value == null)
                {
                    writer.Write(str.Version);
                }
                else
                {
                    WriteString(str.Value ?? string.Empty, true);
                }

                WriteString(str.Handle, true);
                break;
            }

            case NodeAttribute.DataType.DT_ScratchBuffer:
            {
                var buffer = (byte[])attr.Value;
                writer.Write((uint)buffer.Length);
                writer.Write(buffer);
                break;
            }

            // DT_TranslatedFSString not supported in LSB
            default:
                BinUtils.WriteAttribute(writer, attr);
                break;
        }
    }

    private void CollectStaticStrings(Resource? resource)
    {
        staticStrings.Clear();
        foreach (var rgn in resource.Regions)
        {
            AddStaticString(rgn.Key);
            CollectStaticStrings(rgn.Value);
        }
    }

    private void CollectStaticStrings(Node? node)
    {
        AddStaticString(node.Name);

        foreach (var attr in node.Attributes)
        {
            AddStaticString(attr.Key);
        }

        foreach (var child in node.Children.SelectMany(children => children.Value))
        {
            CollectStaticStrings(child);
        }
    }

    private void AddStaticString(string? s)
    {
        if (!staticStrings.ContainsKey(s))
        {
            staticStrings.Add(s, nextStaticStringId++);
        }
    }

    private void WriteStaticStrings()
    {
        writer.Write((uint)staticStrings.Count);
        foreach (var s in staticStrings)
        {
            WriteString(s.Key, false);
            writer.Write(s.Value);
        }
    }

    private void WriteString(string? s, bool nullTerminated)
    {
        var utf = Encoding.UTF8.GetBytes(s);
        var length = utf.Length
                   + (nullTerminated
                         ? 1
                         : 0);

        writer.Write(length);
        writer.Write(utf);
        if (nullTerminated)
        {
            writer.Write((byte)0);
        }
    }

    private void WriteWideString(string s, bool nullTerminated)
    {
        var unicode = Encoding.Unicode.GetBytes(s);
        var length = unicode.Length / 2
                   + (nullTerminated
                         ? 1
                         : 0);

        writer.Write(length);
        writer.Write(unicode);
        if (nullTerminated)
        {
            writer.Write((ushort)0);
        }
    }
}