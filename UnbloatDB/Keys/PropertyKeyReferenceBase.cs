namespace UnbloatDB.Keys;

public class PropertyKeyReferenceBase<T>
{
    public PropertyKeyReferenceBase(string property, string recordKey)
    {
        Property = property;
        RecordKey = recordKey;
        RecordType = typeof(T);
    }

    public string Property { get; init; }
    public string RecordKey { get; init; }
    public Type RecordType { get; init; }

    public void Deconstruct(out string property, out string recordKey)
    {
        property = Property;
        recordKey = RecordKey;
    }
}