using YamlDotNet.Serialization;

namespace UnbloatDB.Serialisers;

public sealed class YamlSerialiser : SerialiserBase
{
    public ISerializer? Serializer { get; set; }
    public IDeserializer Deserializer { get; set; }

    public YamlSerialiser(ISerializer serializer)
    {
        Serializer = serializer;
        Deserializer = new Deserializer();
    }

    public YamlSerialiser()
    {
    }

    public override ValueTask<string> Serialise<T>(T instance)
    {
        var serializer = Serializer ?? new SerializerBuilder().Build();
        return ValueTask.FromResult(serializer.Serialize(instance));
    }

    public override ValueTask<T> Deserialise<T>(Stream data)
    {
        using var reader = new StreamReader(data);
        var stringData = reader.ReadToEnd();
        
        return ValueTask.FromResult(Deserializer.Deserialize<T>(stringData));
    }
}