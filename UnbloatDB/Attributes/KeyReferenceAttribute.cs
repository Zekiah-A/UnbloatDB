namespace UnbloatDB.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class KeyReferenceAttribute : Attribute
{
    public Type ReferenceTargetType { get; set; }

    public KeyReferenceAttribute()
    {
        // No-action
    }
}