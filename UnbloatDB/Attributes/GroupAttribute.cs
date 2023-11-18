namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class GroupAttribute : Attribute
{
    public string? GroupName { get; set; }

    public GroupAttribute()
    {
        // No-action
    }
}