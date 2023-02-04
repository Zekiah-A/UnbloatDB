namespace UnbloatDB.Keys;

public interface IKeyReferenceBase
{
    public string Key { get; }
    public bool ReferenceDeleted { get; set; }
}