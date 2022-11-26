using System.Reflection.Metadata;
using System.Text.Json;

namespace UnbloatDB.Serialisers;

public sealed class JsonSerialiser : SerialiserBase
{
    public JsonSerializerOptions? Options { get; set; }

    public JsonSerialiser(JsonSerializerOptions options)
    {
        Options = options;
    }

    public JsonSerialiser()
    {
    }

    public override Task<string> Serialise<T>(T instance)
    {
        return Task.FromResult(JsonSerializer.Serialize(instance, Options));
    }

    public override Task<T> Deserialise<T>(Stream data)
    {
        return Task.FromResult(JsonSerializer.Deserialize<T>(data));
    }
}