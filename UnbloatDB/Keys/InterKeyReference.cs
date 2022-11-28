namespace UnbloatDB.Keys;

public sealed record InterKey(string RecordKey, string Group, bool ReferenceDeleted) : KeyReferenceBase(RecordKey, ReferenceDeleted);