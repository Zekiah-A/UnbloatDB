namespace UnbloatDB.Keys;

public sealed record IntraKey<T>(string RecordKey, string Group) : KeyReferenceBase<T>(RecordKey);