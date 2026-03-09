using System.Collections.Generic;
using Ate.Contracts;
using Ate.Engine.Configuration;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;
using Ate.Engine.Wrappers;

namespace Ate.Engine.DeviceIntegration.Providers;

public sealed class PsuConfiguredWrapperProvider : IConfiguredWrapperProvider
{
    private readonly IPsuHardwareDriverFactory _hardwareDriverFactory;

    public PsuConfiguredWrapperProvider(IPsuHardwareDriverFactory hardwareDriverFactory)
    {
        _hardwareDriverFactory = hardwareDriverFactory;
    }

    public string Name => "PSU";

    public bool CanCreate(DriverInstanceConfiguration configuration)
    {
        return configuration.DeviceType.Equals("PSU", System.StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        var endpoint = ConnectionEndpointResolver.Resolve(configuration);
        var wrapper = new PsuDeviceWrapper(configuration.DriverId, configuration.Ip, configuration.Channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = BuildDefinition(configuration),
            Description = $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{configuration.Channel}"
        };
    }

    private static DeviceCommandDefinition BuildDefinition(DriverInstanceConfiguration cfg)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = cfg.DeviceType,
            DriverId = cfg.DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition
                {
                    Name = "channel",
                    Type = ParameterValueType.Integer,
                    IsRequired = true,
                    DefaultValue = cfg.Channel.ToString()
                }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "SetVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "voltage", Type = ParameterValueType.Decimal, IsRequired = true, DefaultValue = "5.0" },
                        new CommandParameterDefinition { Name = "currentLimit", Type = ParameterValueType.Decimal, DefaultValue = "1.0" }
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetCurrentLimit",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "currentLimit", Type = ParameterValueType.Decimal, IsRequired = true, DefaultValue = "1.0" }
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetOutput",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "enabled", Type = ParameterValueType.Boolean, DefaultValue = "true" }
                    }
                },
                new CommandOperationDefinition { Name = "OutputOff" },
                new CommandOperationDefinition { Name = "Identify" }
            }
        };
    }
}
