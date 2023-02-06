// TODO: Find a way to integrate this
/*
using BinaryPack;

namespace UnbloatDB.Serialisers;

public class BinarySerializer : SerialiserBase
{
    public BinarySerializer()
    {
    }

    public override ValueTask<string> Serialise<T>(T instance)
    {
        return Task.FromResult(BinaryConverter.Serialize(instance));
    }
    
    public override ValueTask<T> Deserialise<T>(Stream data)
    {
        return Task.FromResult(BinaryConverter.Deserialize<T>(data));
    }
}
*/