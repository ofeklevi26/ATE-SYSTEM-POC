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
            if (operation.Equals("MeasureVoltage", StringComparison.OrdinalIgnoreCase))
            {
                var range = ReadDecimal(parameters, "range", 10.0m);
                var value = _device.MeasureVoltage(Ip, Channel, range);
                return Task.FromResult<object>(new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" });
            }

            if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(_device.Identify(Ip, Channel));
            }

            throw new InvalidOperationException($"Unsupported DMM operation '{operation}'.");
        }
        finally
        {
            _device.Disconnect();
        }
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
