namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class DoNotIndexAttribute : Attribute
{
    public DoNotIndexAttribute()
    {
        // No-action
    }
}
