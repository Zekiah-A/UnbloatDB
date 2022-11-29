namespace UnbloatDB;

public sealed record RecordStructure<T>(string MasterKey, T Data) where T : notnull;