namespace UnbloatDB.Keys;

internal record PropertyIntraKeyReference<T>
(
    string Property,
    string RecordKey
) : IntraKeyReference<T>(RecordKey);