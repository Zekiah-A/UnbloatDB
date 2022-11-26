namespace UnbloatDB.Keys;

public record IntraKey(string RecordKey, bool ReferenceDeleted) : KeyReferenceBase(RecordKey, ReferenceDeleted);