using System.Text.Json.Serialization;

namespace UnbloatDB;

public sealed partial record RecordStructure<T> where T : notnull
{
    [JsonInclude]
    public string MasterKey { get; private set; }
    public T Data { get; set; }
    public Dictionary<Type, string> Referencers { get; set; }

    // Some serialisers require parameterless constructor
    public RecordStructure() { }

    public RecordStructure(string masterKey, T data)
    {
        MasterKey = masterKey;
        Data = data;
        Referencers = new Dictionary<Type, string>();
    }
}