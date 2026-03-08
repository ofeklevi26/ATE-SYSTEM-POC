using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Contracts;
using Ate.Engine.Drivers;

namespace Ate.Engine.BuiltInDrivers;

public sealed class DmmDemoDriver : IDeviceDriver
{
    public string DeviceType => "DMM";

    public string DriverId => "default";

    public DeviceCommandDefinition GetCommandDefinition()
    {
        return new DeviceCommandDefinition
        {
            DeviceType = DeviceType,
            DriverId = DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition { Name = "ip", Type = ParameterValueType.String, IsRequired = true, DefaultValue = "192.168.0.10" }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "MeasureVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "channel", Type = ParameterValueType.Integer, IsRequired = true, DefaultValue = "1" },
                        new CommandParameterDefinition { Name = "range", Type = ParameterValueType.Decimal, DefaultValue = "10.0" }
                    }
                },
                new CommandOperationDefinition { Name = "Identify" }
            }
        };
    }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var ip = ReadString(parameters, "ip", required: true);
        var adapter = new DmmAdapter(ip);

        if (operation.Equals("MeasureVoltage", StringComparison.OrdinalIgnoreCase))
        {
            var channel = ReadInt(parameters, "channel", 1);
            var range = ReadDecimal(parameters, "range", 10.0m);
            var value = adapter.MeasureVoltage(channel, range);
            return Task.FromResult<object>(new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" });
        }

        if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>(adapter.Identify());
        }

        throw new InvalidOperationException($"Unsupported DMM operation '{operation}'.");
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

        return decimal.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private sealed class DmmAdapter
    {
        private readonly string _ip;

        public DmmAdapter(string ip)
        {
            _ip = ip;
        }

        public string Identify()
        {
            return $"DMM-REAL-ADAPTER@{_ip}";
        }

        public decimal MeasureVoltage(int channel, decimal range)
        {
            var seed = (_ip.GetHashCode() & 0x7fffffff) % 100;
            return 3.0m + channel * 0.1m + (range > 10 ? 0.05m : 0m) + seed / 1000m;
        }
    }
}
