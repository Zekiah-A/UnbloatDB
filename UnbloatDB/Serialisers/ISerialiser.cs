namespace UnbloatDB.Serialisers;

internal interface ISerialiser
{
    public ValueTask<string> Serialise<T>(T instance) where T : notnull;

    public ValueTask<T> Deserialise<T>(Stream data) where T : notnull;
}