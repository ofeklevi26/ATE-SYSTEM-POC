using System;
using System.Collections.Generic;
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

        if (operation.Equals("SetVoltage", StringComparison.OrdinalIgnoreCase))
        {
            parameters.TryGetValue("voltage", out var voltage);
            return Task.FromResult<object>($"PSU voltage set to {voltage ?? "unknown"} V");
        }

        if (operation.Equals("OutputOn", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<object>("PSU output enabled");
        }

        throw new InvalidOperationException($"Unsupported PSU operation '{operation}'.");
    }
}
