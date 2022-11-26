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
}