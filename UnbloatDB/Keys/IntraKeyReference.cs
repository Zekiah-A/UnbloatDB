namespace UnbloatDB.Keys;

public sealed record IntraKey<T>(string RecordKey, bool ReferenceDeleted) : KeyReferenceBase<T>(RecordKey, ReferenceDeleted);