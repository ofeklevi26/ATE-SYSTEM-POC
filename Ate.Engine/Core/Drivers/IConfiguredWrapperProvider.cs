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
    public IDeviceDriver Driver { get; set; } = null!;

    public DeviceCommandDefinition Definition { get; set; } = null!;

    public string? Description { get; set; }
}
