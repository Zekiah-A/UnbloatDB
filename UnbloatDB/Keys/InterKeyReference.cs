namespace UnbloatDB.Keys;

public sealed record InterKey<T>(string RecordKey) : KeyReferenceBase<T>(RecordKey);