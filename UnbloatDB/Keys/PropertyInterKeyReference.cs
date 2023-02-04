namespace UnbloatDB.Keys;

public record PropertyInterKeyReference<T>
(
    string Property,
    string RecordKey,
    string Group
) : InterKeyReference<T>(RecordKey, Group);