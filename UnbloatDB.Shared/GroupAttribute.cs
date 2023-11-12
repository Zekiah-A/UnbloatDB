using System;

namespace UnbloatDB.Shared;

[AttributeUsage(AttributeTargets.Class)]
public class GroupAttribute : Attribute
{
    public string? GroupName { get; set; }

    public GroupAttribute()
    {
        // No-action
    }
}