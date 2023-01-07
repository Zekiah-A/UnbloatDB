namespace UnbloatDB.Keys;

public record KeyReferenceBase<T>(string Key)
{
    public bool ReferenceDeleted { get; set; } = false;

    public static implicit operator string(KeyReferenceBase<T> reference) => reference.Key;
}