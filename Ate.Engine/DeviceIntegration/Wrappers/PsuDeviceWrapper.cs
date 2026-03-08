using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;

namespace Ate.Engine.Wrappers;

public sealed class PsuDeviceWrapper : IDeviceDriver
{
    private readonly IPsuHardwareDriver _hardware;

    public PsuDeviceWrapper(string driverId, string ip, int channel, string endpoint, IPsuHardwareDriver hardware)
    {
        DriverId = driverId;
        Ip = ip;
        Channel = channel;
        Endpoint = endpoint;
        _hardware = hardware;
    }

    public string DeviceType => "PSU";

    public string DriverId { get; }

    public string Ip { get; }

    public int Channel { get; }

    public string Endpoint { get; }

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        _hardware.Connect(Endpoint);
        try
        {
            var channel = ReadInt(parameters, "channel", Channel);

            if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<object>(_hardware.Identify(Ip, channel));
            }

            if (operation.Equals("SetVoltage", StringComparison.OrdinalIgnoreCase))
            {
                var voltage = ReadDecimal(parameters, "voltage", 5.0m);
                var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
                _hardware.SetVoltage(channel, voltage, currentLimit);
                return Task.FromResult<object>($"PSU configured: Voltage={voltage.ToString("0.###", CultureInfo.InvariantCulture)}V, CurrentLimit={currentLimit.ToString("0.###", CultureInfo.InvariantCulture)}A on CH{channel}");
            }

            if (operation.Equals("SetCurrentLimit", StringComparison.OrdinalIgnoreCase))
            {
                var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
                _hardware.SetCurrentLimit(channel, currentLimit);
                return Task.FromResult<object>($"PSU current limit set to {currentLimit.ToString("0.###", CultureInfo.InvariantCulture)} A on CH{channel}");
            }

            if (operation.Equals("SetOutput", StringComparison.OrdinalIgnoreCase))
            {
                var enabled = ReadBool(parameters, "enabled", true);
                _hardware.SetOutput(channel, enabled);
                return Task.FromResult<object>(enabled ? $"PSU output enabled on CH{channel}" : $"PSU output disabled on CH{channel}");
            }

            if (operation.Equals("OutputOff", StringComparison.OrdinalIgnoreCase))
            {
                _hardware.SetOutput(channel, false);
                return Task.FromResult<object>($"PSU output disabled on CH{channel}");
            }

            throw new InvalidOperationException($"Unsupported PSU operation '{operation}'. Supported operations: Identify, SetVoltage, SetCurrentLimit, SetOutput, OutputOff.");
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
}
