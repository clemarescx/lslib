using System;
using System.IO;
using Newtonsoft.Json;

namespace LSLib.LS;

public class LSJReader : ILSReader
{
    private Stream stream;
    private JsonTextReader reader;

    public LSJReader(Stream stream)
    {
        this.stream = stream;
    }

    public void Dispose()
    {
        stream.Dispose();
    }

    public Resource Read()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LSJResourceConverter());
        var serializer = JsonSerializer.Create(settings);

        using var streamReader = new StreamReader(stream);
        using (reader = new(streamReader))
        {
            return serializer.Deserialize<Resource>(reader);
        }
    }
}