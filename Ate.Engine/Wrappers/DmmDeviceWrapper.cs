using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class DmmDeviceWrapper : IDeviceDriver
{
    private readonly IDmmHardwareDriver _hardware;

    public DmmDeviceWrapper(string driverId, string ip, int channel, IDmmHardwareDriver hardware)
    {
        DriverId = driverId;
        Ip = ip;
        Channel = channel;
        _hardware = hardware;
    }

    public string DeviceType => "DMM";

    public string DriverId { get; }

    public string Ip { get; }

    public int Channel { get; }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        _hardware.Connect(Ip);
        try
        {
            var channel = ReadInt(parameters, "channel", Channel);

            if (operation.Equals("MeasureVoltage", StringComparison.OrdinalIgnoreCase))
            {
                var range = ReadDecimal(parameters, "range", 10.0m);
                var value = _hardware.MeasureVoltage(Ip, channel, range);
                return Task.FromResult<object>(new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" });
            }

            if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(_hardware.Identify(Ip, channel));
            }

            throw new InvalidOperationException($"Unsupported DMM operation '{operation}'.");
        }
        finally
        {
            _hardware.Disconnect();
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
