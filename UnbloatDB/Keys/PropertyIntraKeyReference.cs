namespace UnbloatDB.Keys;

public record PropertyIntraKeyReference<T>
(
    string Property,
    string RecordKey
) : IntraKeyReference<T>(RecordKey);