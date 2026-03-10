using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ate.Contracts;

namespace Ate.Engine.Drivers;

public static class WrapperOperationRuntime
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, MethodInfo>> OperationCache =
        new ConcurrentDictionary<Type, IReadOnlyDictionary<string, MethodInfo>>();

    public static DeviceCommandDefinition BuildDefinition(IDeviceDriver driver, IEnumerable<CommandParameterDefinition>? driverParameters = null)
    {
        var type = driver.GetType();
        var operations = GetOperationMethods(type)
            .Select(kvp => BuildOperationDefinition(kvp.Key, kvp.Value))
            .OrderBy(op => op.Name)
            .ToList();

        return new DeviceCommandDefinition
        {
            DeviceType = driver.DeviceType,
            DriverId = driver.DriverId,
            DriverParameters = driverParameters?.ToList() ?? new List<CommandParameterDefinition>(),
            Operations = operations
        };
    }

    public static Task<object> InvokeAsync(object wrapper, string operation, IReadOnlyDictionary<string, object> parameters, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var operations = GetOperationMethods(wrapper.GetType());
        if (!operations.TryGetValue(operation, out var method))
        {
            throw new InvalidOperationException($"Unsupported operation '{operation}'. Supported operations: {string.Join(", ", operations.Keys.OrderBy(x => x))}.");
        }

        var args = BindParameters(method, parameters);
        var result = method.Invoke(wrapper, args);
        return Task.FromResult(result ?? string.Empty);
    }

    private static CommandOperationDefinition BuildOperationDefinition(string operationName, MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(BuildParameterDefinition)
            .ToList();

        return new CommandOperationDefinition
        {
            Name = operationName,
            Parameters = parameters
        };
    }

    private static CommandParameterDefinition BuildParameterDefinition(ParameterInfo parameter)
    {
        var isNullableValueType = Nullable.GetUnderlyingType(parameter.ParameterType) != null;
        var isRequired = !parameter.HasDefaultValue && !isNullableValueType;
        var defaultValue = parameter.HasDefaultValue && parameter.DefaultValue != null
            ? Convert.ToString(parameter.DefaultValue, CultureInfo.InvariantCulture)
            : null;

        return new CommandParameterDefinition
        {
            Name = parameter.Name ?? string.Empty,
            Type = MapParameterType(parameter.ParameterType),
            IsRequired = isRequired,
            DefaultValue = defaultValue
        };
    }

    private static ParameterValueType MapParameterType(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(int) || effectiveType == typeof(long))
        {
            return ParameterValueType.Integer;
        }

        if (effectiveType == typeof(decimal) || effectiveType == typeof(double) || effectiveType == typeof(float))
        {
            return ParameterValueType.Decimal;
        }

        if (effectiveType == typeof(bool))
        {
            return ParameterValueType.Boolean;
        }

        return ParameterValueType.String;
    }

    private static object?[] BindParameters(MethodInfo method, IReadOnlyDictionary<string, object> provided)
    {
        return method.GetParameters().Select(param =>
        {
            if (provided.TryGetValue(param.Name ?? string.Empty, out var raw) && raw != null)
            {
                return ConvertValue(raw, param.ParameterType);
            }

            if (param.HasDefaultValue)
            {
                return param.DefaultValue;
            }

            if (Nullable.GetUnderlyingType(param.ParameterType) != null)
            {
                return null;
            }

            throw new InvalidOperationException($"Missing required parameter '{param.Name}'.");
        }).ToArray();
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveType.IsInstanceOfType(value))
        {
            return value;
        }

        if (value is string s)
        {
            if (effectiveType == typeof(bool) && bool.TryParse(s, out var boolParsed))
            {
                return boolParsed;
            }

            if (effectiveType == typeof(int) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intParsed))
            {
                return intParsed;
            }

            if (effectiveType == typeof(decimal) && decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var decParsed))
            {
                return decParsed;
            }
        }

        if (effectiveType == typeof(decimal) && (value is double dbl))
        {
            return decimal.Parse(dbl.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        if (effectiveType == typeof(decimal) && (value is float flt))
        {
            return decimal.Parse(flt.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        if (effectiveType == typeof(int) && value is long l && l <= int.MaxValue && l >= int.MinValue)
        {
            return (int)l;
        }

        return Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);
    }

    private static IReadOnlyDictionary<string, MethodInfo> GetOperationMethods(Type wrapperType)
    {
        return OperationCache.GetOrAdd(wrapperType, type =>
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Select(m => new
                {
                    Method = m,
                    Attribute = m.GetCustomAttribute<DriverOperationAttribute>()
                })
                .Where(x => x.Attribute != null)
                .ToDictionary(
                    x => x.Attribute?.Name ?? x.Method.Name,
                    x => x.Method,
                    StringComparer.OrdinalIgnoreCase);

            return methods;
        });
    }
}
