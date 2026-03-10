using System;

namespace Ate.Engine.Drivers;

public sealed class ConfiguredWrapperDescriptor
{
    public ConfiguredWrapperDescriptor(string deviceType, Type wrapperType)
    {
        DeviceType = deviceType;
        WrapperType = wrapperType;
    }

    public string DeviceType { get; }

    public Type WrapperType { get; }
}
