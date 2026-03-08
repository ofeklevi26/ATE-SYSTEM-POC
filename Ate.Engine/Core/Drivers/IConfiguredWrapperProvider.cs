using Ate.Contracts;
using Ate.Engine.Configuration;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Drivers;

public interface IConfiguredWrapperProvider
{
    string Name { get; }

    bool CanCreate(DriverInstanceConfiguration configuration);

    ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger);
}

public sealed class ConfiguredWrapperRegistration
{
    public required IDeviceDriver Driver { get; init; }

    public required DeviceCommandDefinition Definition { get; init; }

    public string? Description { get; init; }
}
