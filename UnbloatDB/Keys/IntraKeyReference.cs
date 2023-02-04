namespace UnbloatDB.Keys;

public record IntraKeyReference<T>(string RecordKey) : KeyReferenceBase<T>(RecordKey);