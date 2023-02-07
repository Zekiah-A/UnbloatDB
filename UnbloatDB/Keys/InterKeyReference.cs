namespace UnbloatDB.Keys;

public record InterKeyReference<T>(string RecordKey) : KeyReferenceBase<T>(RecordKey);