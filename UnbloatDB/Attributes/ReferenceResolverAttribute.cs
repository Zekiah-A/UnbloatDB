namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class ReferenceResolverAttribute : Attribute
{
    public ReferenceResolverAttribute()
    {
        // No-action
    }
}