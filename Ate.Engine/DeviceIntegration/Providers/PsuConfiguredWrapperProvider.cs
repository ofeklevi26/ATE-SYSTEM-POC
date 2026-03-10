using System;
using System.Collections.Generic;
using Ate.Engine.Configuration;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.DeviceIntegration.Providers;

public sealed class PsuConfiguredWrapperProvider : IConfiguredWrapperProvider
{
    private readonly IPsuHardwareDriverFactory _hardwareDriverFactory;

    public PsuConfiguredWrapperProvider(IPsuHardwareDriverFactory hardwareDriverFactory)
    {
        _hardwareDriverFactory = hardwareDriverFactory;
    }

    public string Name => "PSU";

    public bool CanCreate(DriverInstanceConfiguration configuration)
    {
        return configuration.DeviceType.Equals("PSU", StringComparison.OrdinalIgnoreCase);
    }

    public ConfiguredWrapperValidationResult Validate(DriverInstanceConfiguration configuration)
    {
        if (!TryResolveAddress(configuration, out _))
        {
            return ConfiguredWrapperValidationResult.Fail("Missing address. Set settings.address or settings.resourceName.");
        }

        return ConfiguredWrapperValidationResult.Success();
    }

    public ConfiguredWrapperRegistration Create(DriverInstanceConfiguration configuration, ILogger logger)
    {
        var address = ResolveAddress(configuration);
        var channel = ResolveChannel(configuration);
        var endpoint = BuildEndpoint(configuration, address, channel);

        var wrapper = new PsuDeviceWrapper(configuration.DriverId, address, channel, endpoint, _hardwareDriverFactory.Create());

        return new ConfiguredWrapperRegistration
        {
            Driver = wrapper,
            Definition = WrapperOperationRuntime.BuildDefinition(wrapper)
        };
    }

    public string Describe(DriverInstanceConfiguration configuration)
    {
        if (!TryResolveAddress(configuration, out var address))
        {
            return $"{Name}::{configuration.DriverId}";
        }

        var channel = ResolveChannel(configuration);
        var endpoint = BuildEndpoint(configuration, address, channel);
        return $"{Name}::{configuration.DriverId} endpoint='{endpoint}' CH{channel}";
    }

    private static string ResolveAddress(DriverInstanceConfiguration configuration)
    {
        if (TryResolveAddress(configuration, out var address))
        {
            return address;
        }

        throw new InvalidOperationException("PSU provider requires settings.address or settings.resourceName.");
    }

    private static bool TryResolveAddress(DriverInstanceConfiguration configuration, out string address)
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

    private static int ResolveChannel(DriverInstanceConfiguration configuration)
    {
        if (TryGetSetting(configuration.Settings, "channel", out var channelRaw) && int.TryParse(channelRaw, out var channel))
        {
            return channel;
        }

        return 1;
    }

    private static string BuildEndpoint(DriverInstanceConfiguration configuration, string address, int channel)
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
