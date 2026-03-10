using System;
using System.Collections.Generic;
using Ate.Engine.Configuration;

namespace Ate.Engine.DeviceIntegration.Providers;

internal static class DmmConnectionSettings
{
    public static bool TryResolveAddress(DriverInstanceConfiguration configuration, out string address)
    {
        if (TryGetSetting(configuration.Settings, "address", out address))
        {
            return true;
        }

        if (TryGetSetting(configuration.Settings, "resourceName", out address))
        {
            return true;
        }

        address = string.Empty;
        return false;
    }

    public static int ResolveChannel(DriverInstanceConfiguration configuration)
    {
        if (TryGetSetting(configuration.Settings, "channel", out var channelRaw) && int.TryParse(channelRaw, out var channel))
        {
            return channel;
        }

        return 1;
    }

    public static string BuildEndpoint(DriverInstanceConfiguration configuration, string address, int channel)
    {
        if (TryGetSetting(configuration.Settings, "endpoint", out var endpoint))
        {
            return endpoint;
        }

        if (TryGetSetting(configuration.Settings, "endpointFormat", out var endpointFormat))
        {
            var tokens = BuildTokens(configuration.Settings, address, channel);
            return ApplyTemplate(endpointFormat, tokens);
        }

        if (!IsResourceBased(configuration.Settings) && TryGetSetting(configuration.Settings, "port", out var port))
        {
            return $"{address}:{port}";
        }

        return address;
    }

    private static bool IsResourceBased(IReadOnlyDictionary<string, string> settings)
    {
        return TryGetSetting(settings, "resourceName", out _);
    }

    private static Dictionary<string, string> BuildTokens(IReadOnlyDictionary<string, string> settings, string address, int channel)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["address"] = address,
            ["channel"] = channel.ToString()
        };

        foreach (var kvp in settings)
        {
            tokens[kvp.Key] = kvp.Value;
        }

        return tokens;
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var output = template;
        foreach (var kvp in tokens)
        {
            output = output.Replace("{" + kvp.Key + "}", kvp.Value);
        }

        return output;
    }

    private static bool TryGetSetting(IReadOnlyDictionary<string, string> settings, string key, out string value)
    {
        if (settings.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }
}
