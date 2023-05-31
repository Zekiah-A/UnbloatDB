namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class GroupAttribute : Attribute
{
    public string? GroupName { get; set; }

    public GroupAttribute()
    {
        // No-action
    }
}
