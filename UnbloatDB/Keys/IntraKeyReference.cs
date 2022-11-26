namespace UnbloatDB.Keys;

public record IntraKey(string RecordKey) : KeyReferenceBase(RecordKey);