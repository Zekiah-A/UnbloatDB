namespace UnbloatDB.Keys;

public class PropertyInterKeyReference<T> : PropertyKeyReferenceBase<T>
{
    public PropertyInterKeyReference(string property, string recordKey, string group) : base(property, recordKey)
    {
        Group = group;
    }

    public string Group { get; init; }

    public void Deconstruct(out string property, out string recordKey, out string group)
    {
        property = Property;
        recordKey = RecordKey;
        group = Group;
    }
}