namespace UnbloatDB.Keys;

public sealed record IntraKey<T>(string RecordKey) : KeyReferenceBase<T>(RecordKey);