namespace UnbloatDB.Keys;

internal record PropertyInterKeyReference<T>
(
    string Property,
    string RecordKey,
    string Group
) : InterKeyReference<T>(RecordKey, Group);