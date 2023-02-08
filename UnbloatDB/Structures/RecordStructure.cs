using UnbloatDB.Keys;

namespace UnbloatDB;

public sealed record RecordStructure<T> where T : notnull
{
    public string MasterKey { get; }
    public T Data { get; set; }
    public List<PropertyKeyReferenceBase> Referencers { get; set; }

    // Some serialisers require parameterless constructor
    public RecordStructure() { }

    public RecordStructure(string masterKey, T data)
    {
        MasterKey = masterKey;
        Data = data;
        Referencers = new List<PropertyKeyReferenceBase>();
    }
}