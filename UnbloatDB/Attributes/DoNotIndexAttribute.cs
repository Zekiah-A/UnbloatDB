namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DoNotIndexAttribute : Attribute
{
    public DoNotIndexAttribute()
    {
        // No-action
    }
}
