using System.Xml;

namespace LSLibLite.LS;

public class LocaXmlWriter
{
    #region Members

    private readonly Stream stream;
    private XmlWriter writer;

    #endregion

    #region Constructors

    public LocaXmlWriter(Stream stream)
    {
        this.stream = stream;
    }

    #endregion

    public void Write(LocaResource res)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t"
        };

        using (writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartElement("contentList");

            foreach (var entry in res.Entries)
            {
                writer.WriteStartElement("content");
                writer.WriteAttributeString("contentuid", entry.Key);
                writer.WriteAttributeString("version", entry.Version.ToString());
                writer.WriteString(entry.Text);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.Flush();
        }
    }
}