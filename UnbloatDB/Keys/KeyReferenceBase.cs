namespace UnbloatDB.Keys;

public record KeyReferenceBase<T>(string RecordKey)
{
    public bool ReferenceDeleted { get; set; } = false;
    public static implicit operator string(KeyReferenceBase<T> reference) => reference.RecordKey;
}