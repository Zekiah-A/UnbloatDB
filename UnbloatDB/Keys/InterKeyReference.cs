namespace UnbloatDB.Keys;

public sealed record InterKey<T>(string RecordKey, bool ReferenceDeleted) : KeyReferenceBase<T>(RecordKey, ReferenceDeleted);