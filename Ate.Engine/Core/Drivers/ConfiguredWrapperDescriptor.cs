using System;

namespace Ate.Engine.Drivers;

public sealed class ConfiguredWrapperDescriptor
{
    public ConfiguredWrapperDescriptor(string deviceType, Type wrapperType)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
        {
            throw new ArgumentException("Device type is required.", nameof(deviceType));
        }

        if (wrapperType == null)
        {
            throw new ArgumentNullException(nameof(wrapperType));
        }

        if (!typeof(IDeviceDriver).IsAssignableFrom(wrapperType))
        {
            throw new ArgumentException($"Wrapper type '{wrapperType.FullName}' must implement {nameof(IDeviceDriver)}.", nameof(wrapperType));
        }

        DeviceType = deviceType.Trim();
        WrapperType = wrapperType;
    }

    public string DeviceType { get; }

    public Type WrapperType { get; }
}
