using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Contracts;
using Ate.Engine.DemoDevices;
using Ate.Engine.Drivers;

namespace Ate.Engine.BuiltInDrivers;

public sealed class DmmDemoDriver : IDeviceDriver
{
    private readonly DmmDemoDevice _device = new DmmDemoDevice();

    public DmmDemoDriver(string driverId, string ip, int channel)
    {
        DriverId = driverId;
        Ip = ip;
        Channel = channel;
    }

    public string DeviceType => "DMM";

    public string DriverId { get; }

    public string Ip { get; }

    public int Channel { get; }

    public DeviceCommandDefinition GetCommandDefinition()
    {
        return new DeviceCommandDefinition
        {
            DeviceType = DeviceType,
            DriverId = DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition { Name = "channel", Type = ParameterValueType.Integer, IsRequired = true, DefaultValue = Channel.ToString() }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "MeasureVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
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

        _device.Connect(Ip);
        try
        {
            var channel = ReadInt(parameters, "channel", Channel);

            if (operation.Equals("MeasureVoltage", StringComparison.OrdinalIgnoreCase))
            {
                var range = ReadDecimal(parameters, "range", 10.0m);
                var value = _device.MeasureVoltage(Ip, channel, range);
                return Task.FromResult<object>(new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" });
            }

            if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(_device.Identify(Ip, channel));
            }

            throw new InvalidOperationException($"Unsupported DMM operation '{operation}'.");
        }
        finally
        {
            _device.Disconnect();
        }
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

        if (value is long l && l <= int.MaxValue && l >= int.MinValue)
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
}
