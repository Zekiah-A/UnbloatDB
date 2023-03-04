namespace UnbloatDB.Keys;

public class PropertyIntraKeyReference<T> : PropertyKeyReferenceBase<T>
{
    public PropertyIntraKeyReference(string Property, string RecordKey) : base(Property, RecordKey)
    {
    }

    public void Deconstruct(out string property, out string recordKey)
    {
        property = Property;
        recordKey = RecordKey;
    }
}