using Newtonsoft.Json;

namespace LSLibLite.LS.Resources.LSJ;

public sealed class LSJReader : ILSReader
{
    #region Members

    private JsonTextReader reader;
    private readonly Stream? stream;

    #endregion

    #region Constructors

    public LSJReader(Stream? stream)
    {
        this.stream = stream;
    }

    #endregion

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource? Read()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LSJResourceConverter());
        var serializer = JsonSerializer.Create(settings);

        using var streamReader = new StreamReader(stream);
        using (reader = new JsonTextReader(streamReader))
        {
            return serializer.Deserialize<Resource>(reader);
        }
    }
}