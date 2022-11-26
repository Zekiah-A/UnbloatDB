namespace UnbloatDB.Keys;

public record InterKey(string RecordKey, string Group, bool ReferenceDeleted) : KeyReferenceBase(RecordKey, ReferenceDeleted);