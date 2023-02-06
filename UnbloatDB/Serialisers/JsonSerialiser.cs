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

    public override ValueTask<string> Serialise<T>(T instance)
    {
        return ValueTask.FromResult(JsonSerializer.Serialize(instance, Options));
    }

    public override ValueTask<T> Deserialise<T>(Stream data)
    {
        return ValueTask.FromResult(JsonSerializer.Deserialize<T>(data));
    }
}