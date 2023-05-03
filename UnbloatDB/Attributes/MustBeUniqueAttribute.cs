namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class MustBeUniqueAttribute : Attribute
{
    public MustBeUniqueAttribute()
    {
        // No-action
    }
}