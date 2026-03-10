using Ate.Contracts;
using Ate.Engine.Configuration;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Drivers;

public interface IConfiguredWrapperProvider
{
    string Name { get; }

    bool CanCreate(DriverInstanceConfiguration configuration);

    ConfiguredWrapperValidationResult Validate(DriverInstanceConfiguration configuration);

    ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger);

    string Describe(DriverInstanceConfiguration configuration);
}

public sealed class ConfiguredWrapperValidationResult
{
    private ConfiguredWrapperValidationResult(bool isValid, string? error)
    {
        IsValid = isValid;
        Error = error;
    }

    public bool IsValid { get; }

    public string? Error { get; }

    public static ConfiguredWrapperValidationResult Success()
    {
        return new ConfiguredWrapperValidationResult(true, null);
    }

    public static ConfiguredWrapperValidationResult Fail(string error)
    {
        return new ConfiguredWrapperValidationResult(false, error);
    }
}

public sealed class ConfiguredWrapperRegistration
{
    public IDeviceDriver Driver { get; set; } = null!;

    public DeviceCommandDefinition Definition { get; set; } = null!;
}
