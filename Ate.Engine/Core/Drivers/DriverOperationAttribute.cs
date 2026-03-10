using System;

namespace Ate.Engine.Drivers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DriverOperationAttribute : Attribute
{
    public DriverOperationAttribute(string? name = null)
    {
        Name = name;
    }

    public string? Name { get; }
}
