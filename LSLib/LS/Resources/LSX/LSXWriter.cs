using LSLib.LS.Enums;
using System.IO;
using System.Linq;
using System.Xml;

namespace LSLib.LS;

public sealed class LSXWriter : ILSWriter
{
    private Stream stream;
    private XmlWriter writer;

    public bool PrettyPrint = false;
    public LSXVersion Version = LSXVersion.V3;

    public LSXWriter(Stream stream)
    {
        this.stream = stream;
    }

    public void Write(Resource resource)
    {
        var settings = new XmlWriterSettings
        {
            Indent = PrettyPrint,
            IndentChars = "\t"
        };

        using (writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("save");

            writer.WriteStartElement("version");

            writer.WriteAttributeString("major", resource.Metadata.MajorVersion.ToString());
            writer.WriteAttributeString("minor", resource.Metadata.MinorVersion.ToString());
            writer.WriteAttributeString("revision", resource.Metadata.Revision.ToString());
            writer.WriteAttributeString("build", resource.Metadata.BuildNumber.ToString());
            writer.WriteEndElement();

            WriteRegions(resource);

            writer.WriteEndElement();
            writer.Flush();
        }
    }

    private void WriteRegions(Resource resource)
    {
        foreach (var region in resource.Regions)
        {
            writer.WriteStartElement("region");
            writer.WriteAttributeString("id", region.Key);
            WriteNode(region.Value);
            writer.WriteEndElement();
        }
    }

    private void WriteTranslatedFSString(TranslatedFSString fs)
    {
        writer.WriteStartElement("string");
        writer.WriteAttributeString("value", fs.Value);
        WriteTranslatedFSStringInner(fs);
        writer.WriteEndElement();
    }

    private void WriteTranslatedFSStringInner(TranslatedFSString fs)
    {
        writer.WriteAttributeString("handle", fs.Handle);
        writer.WriteAttributeString("arguments", fs.Arguments.Count.ToString());

        if (fs.Arguments.Count <= 0)
        {
            return;
        }

        writer.WriteStartElement("arguments");
        foreach (var argument in fs.Arguments)
        {
            writer.WriteStartElement("argument");
            writer.WriteAttributeString("key", argument.Key);
            writer.WriteAttributeString("value", argument.Value);
            WriteTranslatedFSString(argument.String);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private void WriteNode(Node node)
    {
        writer.WriteStartElement("node");
        writer.WriteAttributeString("id", node.Name);

        foreach (var attribute in node.Attributes)
        {
            writer.WriteStartElement("attribute");
            writer.WriteAttributeString("id", attribute.Key);
            if (Version >= LSXVersion.V4)
            {
                writer.WriteAttributeString("type", AttributeTypeMaps.IdToType[attribute.Value.Type]);
            }
            else
            {
                writer.WriteAttributeString("type", ((int)attribute.Value.Type).ToString());
            }

            switch (attribute.Value.Type)
            {
                case NodeAttribute.DataType.DT_TranslatedString:
                {
                    var ts = (TranslatedString)attribute.Value.Value;
                    writer.WriteAttributeString("handle", ts.Handle);
                    if (ts.Value != null)
                    {
                        writer.WriteAttributeString("value", ts.ToString());
                    }
                    else
                    {
                        writer.WriteAttributeString("version", ts.Version.ToString());
                    }

                    break;
                }

                case NodeAttribute.DataType.DT_TranslatedFSString:
                {
                    var fs = (TranslatedFSString)attribute.Value.Value;
                    writer.WriteAttributeString("value", fs.Value);
                    WriteTranslatedFSStringInner(fs);
                    break;
                }

                default:
                    // Replace bogus 001F characters found in certain LSF nodes
                    writer.WriteAttributeString("value", attribute.Value.ToString().Replace("\x1f", ""));
                    break;
            }

            writer.WriteEndElement();
        }

        if (node.ChildCount > 0)
        {
            writer.WriteStartElement("children");
            foreach (var child in node.Children.SelectMany(children => children.Value))
            {
                WriteNode(child);
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
    }
}