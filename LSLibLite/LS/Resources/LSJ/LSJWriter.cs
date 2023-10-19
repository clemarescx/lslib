using Newtonsoft.Json;

namespace LSLibLite.LS.Resources.LSJ;

public sealed class LSJWriter : ILSWriter
{
    #region Members

    private JsonTextWriter writer;
    private readonly Stream stream;

    #endregion

    #region Constructors

    public LSJWriter(Stream stream)
    {
        this.stream = stream;
    }

    #endregion

    public void Write(Resource? resource)
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