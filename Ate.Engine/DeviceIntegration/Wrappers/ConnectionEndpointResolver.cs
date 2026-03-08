using System;
using Ate.Engine.Configuration;

namespace Ate.Engine.Wrappers;

public static class ConnectionEndpointResolver
{
    public static string Resolve(DriverInstanceConfiguration configuration)
    {
        if (configuration.Settings.TryGetValue("endpoint", out var endpoint) && !string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        if (configuration.Settings.TryGetValue("endpointFormat", out var endpointFormat) && !string.IsNullOrWhiteSpace(endpointFormat))
        {
            return endpointFormat
                .Replace("{ip}", configuration.Ip ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{port}", configuration.Port?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        if (configuration.Port.HasValue && configuration.Port.Value > 0)
        {
            return $"{configuration.Ip}:{configuration.Port.Value}";
        }

        return configuration.Ip;
    }
}
