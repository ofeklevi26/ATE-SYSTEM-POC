using System;
using System.Collections.Generic;

namespace Ate.Engine.Configuration;

public static class DriverConfigurationValueResolver
{
    public static string ResolveAddress(DriverInstanceConfiguration configuration)
    {
        if (TryGetSetting(configuration, "address", out var address))
        {
            return address;
        }

        if (TryGetSetting(configuration, "resourceName", out var resourceName))
        {
            return resourceName;
        }

        if (TryGetSetting(configuration, "ip", out var ipFromSettings))
        {
            return ipFromSettings;
        }

        return string.Empty;
    }

    public static int ResolveChannel(DriverInstanceConfiguration configuration)
    {
        if (TryGetSetting(configuration, "channel", out var channelRaw) &&
            int.TryParse(channelRaw, out var channel))
        {
            return channel;
        }

        return 1;
    }

    public static int? ResolvePort(DriverInstanceConfiguration configuration)
    {
        if (TryGetSetting(configuration, "port", out var portRaw) && int.TryParse(portRaw, out var port) && port > 0)
        {
            return port;
        }

        return null;
    }

    public static bool TryGetSetting(DriverInstanceConfiguration configuration, string key, out string value)
    {
        if (configuration.Settings.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var output = template;
        foreach (var kvp in tokens)
        {
            output = output.Replace("{" + kvp.Key + "}", kvp.Value ?? string.Empty);
        }

        return output;
    }

    public static Dictionary<string, string> BuildDefaultTokens(DriverInstanceConfiguration configuration, string address, int channel, int? port)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["address"] = address,
            ["ip"] = address,
            ["port"] = port?.ToString() ?? string.Empty,
            ["channel"] = channel.ToString()
        };

        foreach (var kvp in configuration.Settings)
        {
            tokens[kvp.Key] = kvp.Value;
        }

        return tokens;
    }
}
