using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

namespace Ate.Engine.BuiltInDrivers;

public sealed class PsuDemoDriver : IDeviceDriver
{
    public string DeviceType => "PSU";

    public string DriverId => "default";

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>("PSU-DEMO-001");
        }

        if (operation.Equals("SetVoltage", StringComparison.OrdinalIgnoreCase))
        {
            var voltage = ReadDecimal(parameters, "voltage", 5.0m);
            var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
            return Task.FromResult<object>($"PSU configured: Voltage={voltage.ToString("0.###", CultureInfo.InvariantCulture)}V, CurrentLimit={currentLimit.ToString("0.###", CultureInfo.InvariantCulture)}A");
        }

        if (operation.Equals("SetCurrentLimit", StringComparison.OrdinalIgnoreCase))
        {
            var currentLimit = ReadDecimal(parameters, "currentLimit", 1.0m);
            return Task.FromResult<object>($"PSU current limit set to {currentLimit.ToString("0.###", CultureInfo.InvariantCulture)} A");
        }

        if (operation.Equals("OutputOn", StringComparison.OrdinalIgnoreCase))
        {
            var enabled = ReadBool(parameters, "state", true);
            return Task.FromResult<object>(enabled ? "PSU output enabled" : "PSU output disabled");
        }

        if (operation.Equals("OutputOff", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>("PSU output disabled");
        }

        if (operation.Equals("SetOutput", StringComparison.OrdinalIgnoreCase))
        {
            var enabled = ReadBool(parameters, "enabled", true);
            return Task.FromResult<object>(enabled ? "PSU output enabled" : "PSU output disabled");
        }

        throw new InvalidOperationException($"Unsupported PSU operation '{operation}'. Supported operations: Identify, SetVoltage, SetCurrentLimit, OutputOn, OutputOff, SetOutput.");
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

        if (decimal.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
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

        if (bool.TryParse(value.ToString(), out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
