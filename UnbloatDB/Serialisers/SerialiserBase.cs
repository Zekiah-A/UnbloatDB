namespace UnbloatDB.Serialisers;

public abstract class SerialiserBase : ISerialiser
{
    public abstract Task<string> Serialise<T>(T instance) where T : notnull;
    public abstract Task<T> Deserialise<T>(Stream data) where T : notnull;
}