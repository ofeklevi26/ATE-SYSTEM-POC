using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Ate.Engine.Configuration;

namespace Ate.Engine.Drivers;

internal static class ConfiguredWrapperFactory
{
    public static IDeviceDriver Create(DriverInstanceConfiguration configuration, Type wrapperType, IServiceProvider services)
    {
        var ctor = SelectConstructor(configuration, wrapperType, services);
        var args = ctor.GetParameters()
            .Select(p => ResolveParameterValue(configuration, p, services))
            .ToArray();

        if (Activator.CreateInstance(wrapperType, args) is not IDeviceDriver wrapper)
        {
            throw new InvalidOperationException($"Wrapper '{wrapperType.FullName}' must implement IDeviceDriver.");
        }

        return wrapper;
    }

    private static ConstructorInfo SelectConstructor(DriverInstanceConfiguration configuration, Type wrapperType, IServiceProvider services)
    {
        var constructors = wrapperType.GetConstructors();
        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"Wrapper '{wrapperType.FullName}' has no public constructor.");
        }

        if (constructors.Length == 1)
        {
            return constructors[0];
        }

        var resolvable = constructors
            .Where(c => c.GetParameters().All(p => CanResolveParameter(configuration, p, services)))
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        if (resolvable.Count == 0)
        {
            throw new InvalidOperationException($"No resolvable constructor found for wrapper '{wrapperType.FullName}'.");
        }

        if (resolvable.Count > 1 && resolvable[0].GetParameters().Length == resolvable[1].GetParameters().Length)
        {
            throw new InvalidOperationException(
                $"Ambiguous constructors for wrapper '{wrapperType.FullName}'. Keep one public constructor or ensure only one best match.");
        }

        return resolvable[0];
    }

    private static bool CanResolveParameter(DriverInstanceConfiguration config, ParameterInfo parameter, IServiceProvider services)
    {
        if (parameter.Name != null && parameter.Name.Equals("driverId", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (parameter.Name != null && config.Settings.ContainsKey(parameter.Name))
        {
            return true;
        }

        if (parameter.Name != null &&
            (parameter.Name.Equals("endpoint", StringComparison.OrdinalIgnoreCase) ||
             parameter.Name.Equals("target", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (services.GetService(parameter.ParameterType) != null)
        {
            return true;
        }

        return parameter.HasDefaultValue;
    }

    private static object? ResolveParameterValue(DriverInstanceConfiguration config, ParameterInfo parameter, IServiceProvider services)
    {
        if (parameter.Name != null && parameter.Name.Equals("driverId", StringComparison.OrdinalIgnoreCase))
        {
            return config.DriverId;
        }

        if (parameter.Name != null && config.Settings.TryGetValue(parameter.Name, out var raw))
        {
            return ConvertToType(parameter.ParameterType, raw);
        }

        if (parameter.Name != null && parameter.Name.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
        {
            return BuildFormattedSetting(config.Settings, "endpoint", "endpointFormat", "address", "channel");
        }

        if (parameter.Name != null && parameter.Name.Equals("target", StringComparison.OrdinalIgnoreCase))
        {
            return BuildFormattedSetting(config.Settings, "target", "targetFormat", "address", "channel");
        }

        var service = services.GetService(parameter.ParameterType);
        if (service != null)
        {
            return service;
        }

        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        throw new InvalidOperationException(
            $"Cannot resolve constructor parameter '{parameter.Name}' for wrapper '{parameter.Member.DeclaringType?.FullName}'.");
    }

    private static object ConvertToType(Type targetType, string raw)
    {
        var nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullable == typeof(string))
        {
            return raw;
        }

        if (nonNullable.IsEnum)
        {
            return Enum.Parse(nonNullable, raw, ignoreCase: true);
        }

        if (nonNullable == typeof(int))
        {
            return int.Parse(raw, CultureInfo.InvariantCulture);
        }

        if (nonNullable == typeof(decimal))
        {
            return decimal.Parse(raw, CultureInfo.InvariantCulture);
        }

        if (nonNullable == typeof(bool))
        {
            return bool.Parse(raw);
        }

        if (nonNullable == typeof(double))
        {
            return double.Parse(raw, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(raw, nonNullable, CultureInfo.InvariantCulture);
    }

    private static string BuildFormattedSetting(
        IReadOnlyDictionary<string, string> settings,
        string valueKey,
        string formatKey,
        string addressKey,
        string channelKey)
    {
        if (settings.TryGetValue(valueKey, out var explicitValue) && !string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue.Trim();
        }

        var address = settings.TryGetValue(addressKey, out var addr) ? addr : string.Empty;
        var channel = settings.TryGetValue(channelKey, out var ch) ? ch : "1";

        if (settings.TryGetValue(formatKey, out var format) && !string.IsNullOrWhiteSpace(format))
        {
            var output = format;
            foreach (var kvp in settings)
            {
                output = output.Replace("{" + kvp.Key + "}", kvp.Value);
            }

            output = output.Replace("{address}", address).Replace("{channel}", channel);
            return output;
        }

        return address;
    }
}
