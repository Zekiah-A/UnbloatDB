namespace UnbloatDB.Keys;

public sealed record IntraKey(string RecordKey, bool ReferenceDeleted) : KeyReferenceBase(RecordKey, ReferenceDeleted);