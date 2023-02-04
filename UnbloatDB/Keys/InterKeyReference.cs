namespace UnbloatDB.Keys;

public record InterKeyReference<T>(string RecordKey, string Group) : KeyReferenceBase<T>(RecordKey);