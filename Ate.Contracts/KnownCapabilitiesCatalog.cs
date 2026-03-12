using System;
using System.Collections.Generic;

namespace Ate.Contracts;

public static class KnownCapabilitiesCatalog
{
    public static bool TryCreateDefinition(string deviceType, string driverId, out DeviceCommandDefinition definition)
    {
        if (deviceType.Equals("DMM", StringComparison.OrdinalIgnoreCase))
        {
            definition = CreateDmmDefinition(driverId);
            return true;
        }

        if (deviceType.Equals("PSU", StringComparison.OrdinalIgnoreCase))
        {
            definition = CreatePsuDefinition(driverId);
            return true;
        }

        definition = null!;
        return false;
    }

    private static DeviceCommandDefinition CreateDmmDefinition(string driverId)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = "DMM",
            DriverId = driverId,
            DriverClassName = "DmmDeviceWrapper",
            DriverDisplayName = "Digital Multimeter",
            DriverDescription = "SCPI-compatible DMM wrapper with voltage measurement operations.",
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "Identify",
                    DisplayName = "Identify",
                    Description = "Reads instrument identification string.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        BuildChannelParameter()
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "MeasureVoltage",
                    DisplayName = "Measure Voltage",
                    Description = "Measures DC voltage on the selected channel.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition
                        {
                            Name = "range",
                            DisplayName = "Range",
                            Description = "Voltage range in volts used for measurement.",
                            Type = ParameterKind.Number,
                            NumberFormat = NumberFormat.Decimal,
                            Nullable = false,
                            DefaultValue = "10.0"
                        },
                        BuildChannelParameter()
                    }
                }
            }
        };
    }

    private static DeviceCommandDefinition CreatePsuDefinition(string driverId)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = "PSU",
            DriverId = driverId,
            DriverClassName = "PsuDeviceWrapper",
            DriverDisplayName = "Power Supply",
            DriverDescription = "Programmable PSU wrapper for output/voltage/current control.",
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "Identify",
                    DisplayName = "Identify",
                    Description = "Reads instrument identification string.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        BuildChannelParameter()
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetVoltage",
                    DisplayName = "Set Voltage",
                    Description = "Sets output voltage and current limit for a channel.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition
                        {
                            Name = "voltage",
                            DisplayName = "Voltage",
                            Description = "Target output voltage in volts.",
                            Type = ParameterKind.Number,
                            NumberFormat = NumberFormat.Decimal,
                            IsRequired = true,
                            Nullable = false,
                            DefaultValue = "0.0"
                        },
                        new CommandParameterDefinition
                        {
                            Name = "currentLimit",
                            DisplayName = "Current Limit",
                            Description = "Maximum current in amps.",
                            Type = ParameterKind.Number,
                            NumberFormat = NumberFormat.Decimal,
                            Nullable = false,
                            DefaultValue = "1.0"
                        },
                        BuildChannelParameter()
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetCurrentLimit",
                    DisplayName = "Set Current Limit",
                    Description = "Sets output current limit for a channel.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition
                        {
                            Name = "currentLimit",
                            DisplayName = "Current Limit",
                            Description = "Maximum current in amps.",
                            Type = ParameterKind.Number,
                            NumberFormat = NumberFormat.Decimal,
                            IsRequired = true,
                            Nullable = false,
                            DefaultValue = "0.0"
                        },
                        BuildChannelParameter()
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetOutput",
                    DisplayName = "Set Output",
                    Description = "Enables or disables output for a channel.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition
                        {
                            Name = "enabled",
                            DisplayName = "Enabled",
                            Description = "True to enable output, false to disable.",
                            Type = ParameterKind.Boolean,
                            Nullable = false,
                            DefaultValue = "true"
                        },
                        BuildChannelParameter()
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "OutputOff",
                    DisplayName = "Output Off",
                    Description = "Turns output off for a channel.",
                    ReturnType = "Object",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        BuildChannelParameter()
                    }
                }
            }
        };
    }

    private static CommandParameterDefinition BuildChannelParameter()
    {
        return new CommandParameterDefinition
        {
            Name = "channel",
            DisplayName = "Channel",
            Description = "Optional channel override.",
            Type = ParameterKind.Integer,
            Nullable = true,
            DefaultValue = "1"
        };
    }
}
