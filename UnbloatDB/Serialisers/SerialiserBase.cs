namespace UnbloatDB.Serialisers;

public abstract class SerialiserBase : ISerialiser
{
    public abstract Task<string> Serialise<T>(T instance) where T : notnull;
}