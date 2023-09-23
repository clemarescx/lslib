using System.IO;
using Newtonsoft.Json;

namespace LSLib.LS;

public sealed class LSJWriter : ILSWriter
{
    private Stream stream;
    private JsonTextWriter writer;
    public bool PrettyPrint = false;

    public LSJWriter(Stream stream)
    {
        this.stream = stream;
    }

    public void Write(Resource resource)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        settings.Converters.Add(new LSJResourceConverter());
        var serializer = JsonSerializer.Create(settings);

        using var streamWriter = new StreamWriter(stream);
        using (writer = new JsonTextWriter(streamWriter))
        {
            writer.IndentChar = '\t';
            writer.Indentation = 1;
            serializer.Serialize(writer, resource);
        }
    }
}