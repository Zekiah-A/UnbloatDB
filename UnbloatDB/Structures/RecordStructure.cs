using System.Collections;
using UnbloatDB.Keys;

namespace UnbloatDB;

public sealed class RecordStructure<T> where T : notnull
{
    public string MasterKey { get; }
    public T Data { get; set; }
    
    public RecordStructure()
    {
        // Some serialisers require parameterless constructor
    }

    public RecordStructure(string masterKey, T data)
    {
        MasterKey = masterKey;
        Data = data;
    }
    
    public List<PropertyKeyReferenceBase> Referencers { get; set; } = new();
}