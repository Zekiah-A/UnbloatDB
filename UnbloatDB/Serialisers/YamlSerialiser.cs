using YamlDotNet.Serialization;

namespace UnbloatDB.Serialisers;

public sealed class YamlSerialiser : SerialiserBase
{
    public ISerializer? Serializer { get; set; }

    public YamlSerialiser(ISerializer serializer)
    {
        Serializer = serializer;
    }

    public YamlSerialiser()
    {
        
    }

    public override Task<string> Serialise<T>(T instance)
    {
        var serializer = Serializer ?? new SerializerBuilder().Build();
        return Task.FromResult(serializer.Serialize(instance));
    }
}