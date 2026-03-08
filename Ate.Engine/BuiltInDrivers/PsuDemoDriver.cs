using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Contracts;
using Ate.Engine.Drivers;

namespace Ate.Engine.BuiltInDrivers;

public sealed class PsuDemoDriver : IDeviceDriver
{
    public string DeviceType => "PSU";

    public string DriverId => "default";

    public DeviceCommandDefinition GetCommandDefinition()
    {
        return new DeviceCommandDefinition
        {
            DeviceType = DeviceType,
            DriverId = DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition { Name = "ip", Type = ParameterValueType.String, IsRequired = true, DefaultValue = "192.168.0.20" },
                new CommandParameterDefinition { Name = "channel", Type = ParameterValueType.Integer, IsRequired = true, DefaultValue = "1" }
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

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var ip = ReadString(parameters, "ip", required: true);
        var channel = ReadInt(parameters, "channel", 1);
        var adapter = new PsuAdapter(ip, channel);

        if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>(adapter.Identify());
        }

        if (operation.Equals("SetVoltage", StringComparison.OrdinalIgnoreCase))
        {
            var voltage = ReadDecimal(parameters, "voltage", 5.0m);
            var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
            adapter.SetVoltage(voltage, currentLimit);
            return Task.FromResult<object>($"PSU configured: Voltage={voltage.ToString("0.###", CultureInfo.InvariantCulture)}V, CurrentLimit={currentLimit.ToString("0.###", CultureInfo.InvariantCulture)}A on CH{channel}");
        }

        if (operation.Equals("SetCurrentLimit", StringComparison.OrdinalIgnoreCase))
        {
            var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
            adapter.SetCurrentLimit(currentLimit);
            return Task.FromResult<object>($"PSU current limit set to {currentLimit.ToString("0.###", CultureInfo.InvariantCulture)} A on CH{channel}");
        }

        if (operation.Equals("SetOutput", StringComparison.OrdinalIgnoreCase))
        {
            var enabled = ReadBool(parameters, "enabled", true);
            adapter.SetOutput(enabled);
            return Task.FromResult<object>(enabled ? $"PSU output enabled on CH{channel}" : $"PSU output disabled on CH{channel}");
        }

        if (operation.Equals("OutputOff", StringComparison.OrdinalIgnoreCase))
        {
            adapter.SetOutput(false);
            return Task.FromResult<object>($"PSU output disabled on CH{channel}");
        }

        throw new InvalidOperationException($"Unsupported PSU operation '{operation}'. Supported operations: Identify, SetVoltage, SetCurrentLimit, SetOutput, OutputOff.");
    }

    private static string ReadString(IReadOnlyDictionary<string, object> parameters, string key, bool required)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            if (required)
            {
                throw new InvalidOperationException($"Missing required parameter '{key}'.");
            }

            return string.Empty;
        }

        return value.ToString()!;
    }

    private static int ReadInt(IReadOnlyDictionary<string, object> parameters, string key, int fallback)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return (int)l;
        }

        return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static decimal ReadDecimal(IReadOnlyDictionary<string, object> parameters, string key, decimal fallback)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is decimal dec)
        {
            return dec;
        }

        if (value is double dbl)
        {
            return decimal.Parse(dbl.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        if (value is float flt)
        {
            return decimal.Parse(flt.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l)
        {
            return l;
        }

        return decimal.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ReadBool(IReadOnlyDictionary<string, object> parameters, string key, bool fallback)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
        {
            return fallback;
        }

        if (value is bool b)
        {
            return b;
        }

        return bool.TryParse(value.ToString(), out var parsed) ? parsed : fallback;
    }

    private sealed class PsuAdapter
    {
        private readonly string _ip;
        private readonly int _channel;

        public PsuAdapter(string ip, int channel)
        {
            _ip = ip;
            _channel = channel;
        }

        public string Identify()
        {
            return $"PSU-REAL-ADAPTER@{_ip}:CH{_channel}";
        }

        public void SetVoltage(decimal voltage, decimal currentLimit)
        {
            _ = voltage;
            _ = currentLimit;
        }

        public void SetCurrentLimit(decimal currentLimit)
        {
            _ = currentLimit;
        }

        public void SetOutput(bool enabled)
        {
            _ = enabled;
        }
    }
}
