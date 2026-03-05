using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;

namespace Ate.Engine.BuiltInDrivers;

public sealed class DmmDemoDriver : IDeviceDriver
{
    public string DeviceType => "DMM";

    public Task<object> ExecuteAsync(string operation, Dictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        if (operation.Equals("MeasureVoltage", StringComparison.OrdinalIgnoreCase))
        {
            var value = 3.3 + (DateTime.UtcNow.Millisecond % 100) / 1000.0;
            return Task.FromResult<object>(new { Value = value.ToString("F3", CultureInfo.InvariantCulture), Unit = "V" });
        }

        if (operation.Equals("Identify", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>("DMM-DEMO-001");
        }

        throw new InvalidOperationException($"Unsupported DMM operation '{operation}'.");
    }
}
