namespace UnbloatDB.Keys;

public record PropertyInterKeyReference<T>
(
    string Property,
    string RecordKey,
    string Group
) : PropertyKeyReferenceBase(Property, RecordKey);