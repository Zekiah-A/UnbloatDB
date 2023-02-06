namespace UnbloatDB.Serialisers;

public abstract class SerialiserBase : ISerialiser
{
    public abstract ValueTask<string> Serialise<T>(T instance) where T : notnull;
    public abstract ValueTask<T> Deserialise<T>(Stream data) where T : notnull;
}