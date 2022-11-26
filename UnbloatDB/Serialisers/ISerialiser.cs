namespace UnbloatDB.Serialisers;

internal interface ISerialiser
{
    public Task<string> Serialise<T>(T instance) where T : notnull;

    public Task<T> Deserialise<T>(Stream data) where T : notnull;
}