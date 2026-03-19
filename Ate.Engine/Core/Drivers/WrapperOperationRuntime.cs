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
        if (KnownCapabilitiesCatalog.TryCreateDefinition(driver.DeviceType, driver.DriverId, out var knownDefinition))
        {
            ValidateContractConsistency(driver.GetType(), knownDefinition);
            return knownDefinition;
        }

        var type = driver.GetType();
        var operations = GetOperationMethods(type)
            .Select(kvp => BuildOperationDefinition(kvp.Key, kvp.Value))
            .OrderBy(op => op.Name)
            .ToList();

        return new DeviceCommandDefinition
        {
            DeviceType = driver.DeviceType,
            DriverId = driver.DriverId,
            DriverClassName = type.Name,
            DriverDisplayName = driver.DeviceType,
            DriverDescription = string.Empty,
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
        try
        {
            var result = method.Invoke(wrapper, args);
            return Task.FromResult(result ?? string.Empty);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    public static void ValidateParametersForOperation(object wrapper, string operation, IReadOnlyDictionary<string, object> parameters)
    {
        var operations = GetOperationMethods(wrapper.GetType());
        if (!operations.TryGetValue(operation, out var method))
        {
            throw new InvalidOperationException($"Unsupported operation '{operation}'. Supported operations: {string.Join(", ", operations.Keys.OrderBy(x => x))}.");
        }

        BindParameters(method, parameters);
    }


    private static void ValidateContractConsistency(Type wrapperType, DeviceCommandDefinition contractDefinition)
    {
        var reflectedOperations = GetOperationMethods(wrapperType)
            .Select(kvp => BuildOperationDefinition(kvp.Key, kvp.Value))
            .ToDictionary(op => op.Name, StringComparer.OrdinalIgnoreCase);

        var contractOperations = contractDefinition.Operations
            .ToDictionary(op => op.Name, StringComparer.OrdinalIgnoreCase);

        var missingInWrapper = contractOperations.Keys
            .Where(name => !reflectedOperations.ContainsKey(name))
            .OrderBy(x => x)
            .ToList();

        if (missingInWrapper.Count > 0)
        {
            throw new InvalidOperationException($"KnownCapabilitiesCatalog drift for deviceType '{contractDefinition.DeviceType}' on wrapper '{wrapperType.FullName}': operations declared in contract but missing in wrapper: {string.Join(", ", missingInWrapper)}.");
        }

        var missingInContract = reflectedOperations.Keys
            .Where(name => !contractOperations.ContainsKey(name))
            .OrderBy(x => x)
            .ToList();

        if (missingInContract.Count > 0)
        {
            throw new InvalidOperationException($"KnownCapabilitiesCatalog drift for deviceType '{contractDefinition.DeviceType}' on wrapper '{wrapperType.FullName}': operations declared in wrapper but missing in contract: {string.Join(", ", missingInContract)}.");
        }

        foreach (var operationName in contractOperations.Keys.OrderBy(x => x))
        {
            var contractOperation = contractOperations[operationName];
            var reflectedOperation = reflectedOperations[operationName];

            ValidateParameterConsistency(wrapperType, contractDefinition.DeviceType, contractOperation, reflectedOperation);
        }
    }

    private static void ValidateParameterConsistency(
        Type wrapperType,
        string deviceType,
        CommandOperationDefinition contractOperation,
        CommandOperationDefinition reflectedOperation)
    {
        if (contractOperation.Parameters.Count != reflectedOperation.Parameters.Count)
        {
            throw new InvalidOperationException(
                $"KnownCapabilitiesCatalog drift for deviceType '{deviceType}', operation '{contractOperation.Name}' on wrapper '{wrapperType.FullName}': parameter count mismatch (contract={contractOperation.Parameters.Count}, wrapper={reflectedOperation.Parameters.Count}).");
        }

        for (var i = 0; i < contractOperation.Parameters.Count; i++)
        {
            var contractParameter = contractOperation.Parameters[i];
            var reflectedParameter = reflectedOperation.Parameters[i];

            if (!contractParameter.Name.Equals(reflectedParameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"KnownCapabilitiesCatalog drift for deviceType '{deviceType}', operation '{contractOperation.Name}' on wrapper '{wrapperType.FullName}': parameter #{i + 1} name mismatch (contract='{contractParameter.Name}', wrapper='{reflectedParameter.Name}').");
            }

            if (contractParameter.Kind != reflectedParameter.Kind)
            {
                throw new InvalidOperationException(
                    $"KnownCapabilitiesCatalog drift for deviceType '{deviceType}', operation '{contractOperation.Name}', parameter '{contractParameter.Name}' on wrapper '{wrapperType.FullName}': Kind mismatch (contract={contractParameter.Kind}, wrapper={reflectedParameter.Kind}).");
            }

            if (contractParameter.NumberFormat != reflectedParameter.NumberFormat)
            {
                throw new InvalidOperationException(
                    $"KnownCapabilitiesCatalog drift for deviceType '{deviceType}', operation '{contractOperation.Name}', parameter '{contractParameter.Name}' on wrapper '{wrapperType.FullName}': NumberFormat mismatch (contract={contractParameter.NumberFormat}, wrapper={reflectedParameter.NumberFormat}).");
            }

            if (contractParameter.Nullable != reflectedParameter.Nullable)
            {
                throw new InvalidOperationException(
                    $"KnownCapabilitiesCatalog drift for deviceType '{deviceType}', operation '{contractOperation.Name}', parameter '{contractParameter.Name}' on wrapper '{wrapperType.FullName}': Nullable mismatch (contract={contractParameter.Nullable}, wrapper={reflectedParameter.Nullable}).");
            }
        }
    }

    private static CommandOperationDefinition BuildOperationDefinition(string operationName, MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(BuildParameterDefinition)
            .ToList();
        return new CommandOperationDefinition
        {
            Name = operationName,
            DisplayName = operationName,
            Description = string.Empty,
            ReturnType = GetTypeDisplayName(method.ReturnType),
            Parameters = parameters
        };
    }

    private static CommandParameterDefinition BuildParameterDefinition(ParameterInfo parameter)
    {
        var explicitDefault = parameter.HasDefaultValue && parameter.DefaultValue != null
            ? Convert.ToString(parameter.DefaultValue, CultureInfo.InvariantCulture)
            : null;
        var defaultValue = explicitDefault
            ?? GetParameterSpecificDefaultValueString(parameter)
            ?? GetImplicitDefaultValueString(parameter.ParameterType);
        var nullable = !parameter.ParameterType.IsValueType || Nullable.GetUnderlyingType(parameter.ParameterType) != null;

        var name = parameter.Name ?? string.Empty;

        return new CommandParameterDefinition
        {
            Name = name,
            DisplayName = name,
            Description = string.Empty,
            Kind = MapParameterKind(parameter.ParameterType),
            NumberFormat = MapNumberFormat(parameter.ParameterType),
            Nullable = nullable,
            Default = defaultValue
        };
    }


    private static string GetTypeDisplayName(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type);
        if (effectiveType != null)
        {
            return $"{GetTypeDisplayName(effectiveType)}?";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var tickIndex = type.Name.IndexOf('`');
        var genericName = tickIndex >= 0 ? type.Name.Substring(0, tickIndex) : type.Name;
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeDisplayName));
        return $"{genericName}<{genericArgs}>";
    }

    private static ParameterKind MapParameterKind(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(int) || effectiveType == typeof(long))
        {
            return ParameterKind.Integer;
        }

        if (effectiveType == typeof(decimal) || effectiveType == typeof(double) || effectiveType == typeof(float))
        {
            return ParameterKind.Number;
        }

        if (effectiveType == typeof(bool))
        {
            return ParameterKind.Boolean;
        }

        return ParameterKind.String;
    }


    private static NumberFormat? MapNumberFormat(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(decimal))
        {
            return NumberFormat.Decimal;
        }

        if (effectiveType == typeof(float))
        {
            return NumberFormat.Float;
        }

        if (effectiveType == typeof(double))
        {
            return NumberFormat.Double;
        }

        return null;
    }

    private static object?[] BindParameters(MethodInfo method, IReadOnlyDictionary<string, object> provided)
    {
        return method.GetParameters().Select(param =>
        {
            if (provided.TryGetValue(param.Name ?? string.Empty, out var raw) && !IsMissingValue(raw))
            {
                try
                {
                    return ConvertValue(raw, param.ParameterType);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter '{param.Name}' in operation '{method.Name}': {ex.Message}",
                        ex);
                }
            }

            throw new InvalidOperationException($"Missing parameter '{param.Name}' for operation '{method.Name}'.");
        }).ToArray();
    }

    private static bool IsMissingValue(object? value)
    {
        if (value == null)
        {
            return true;
        }

        return value is string s && string.IsNullOrWhiteSpace(s);
    }



    private static string? GetParameterSpecificDefaultValueString(ParameterInfo parameter)
    {
        if ((parameter.Name ?? string.Empty).Equals("channel", StringComparison.OrdinalIgnoreCase))
        {
            return "1";
        }

        return null;
    }

    private static string GetImplicitDefaultValueString(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        if (effectiveType == typeof(bool))
        {
            return "false";
        }

        if (effectiveType == typeof(int) || effectiveType == typeof(long))
        {
            return "0";
        }

        if (effectiveType == typeof(decimal) || effectiveType == typeof(double) || effectiveType == typeof(float))
        {
            return "0.0";
        }

        return string.Empty;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var incomingType = value.GetType().Name;

        if (value == null)
        {
            throw new InvalidOperationException($"Type mismatch: expected '{effectiveType.Name}' but received null.");
        }

        if (effectiveType.IsInstanceOfType(value))
        {
            return value;
        }

        if (effectiveType == typeof(string))
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        if (effectiveType == typeof(bool))
        {
            if (value is string boolString && bool.TryParse(boolString, out var boolParsed))
            {
                return boolParsed;
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType == typeof(int))
        {
            if (value is string intString && int.TryParse(intString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intParsed))
            {
                return intParsed;
            }

            if (value is long l && l <= int.MaxValue && l >= int.MinValue)
            {
                return (int)l;
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType == typeof(decimal))
        {
            if (value is string decimalString && decimal.TryParse(decimalString, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalParsed))
            {
                return decimalParsed;
            }

            if (value is double dbl)
            {
                if (double.IsNaN(dbl) || double.IsInfinity(dbl))
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
                }

                try
                {
                    return Convert.ToDecimal(dbl, CultureInfo.InvariantCulture);
                }
                catch (OverflowException ex)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.",
                        ex);
                }
            }

            if (value is float flt)
            {
                if (float.IsNaN(flt) || float.IsInfinity(flt))
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
                }

                try
                {
                    return Convert.ToDecimal(flt, CultureInfo.InvariantCulture);
                }
                catch (OverflowException ex)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.",
                        ex);
                }
            }

            if (value is int intValue)
            {
                return (decimal)intValue;
            }

            if (value is long longValue)
            {
                return (decimal)longValue;
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType == typeof(long))
        {
            if (value is string longString && long.TryParse(longString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longParsed))
            {
                return longParsed;
            }

            if (value is int intForLong)
            {
                return (long)intForLong;
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType == typeof(double))
        {
            if (value is string doubleString && double.TryParse(doubleString, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleParsed))
            {
                if (!double.IsNaN(doubleParsed) && !double.IsInfinity(doubleParsed))
                {
                    return doubleParsed;
                }
            }

            if (value is float floatForDouble)
            {
                if (!float.IsNaN(floatForDouble) && !float.IsInfinity(floatForDouble))
                {
                    return (double)floatForDouble;
                }
            }

            if (value is int intForDouble)
            {
                return (double)intForDouble;
            }

            if (value is long longForDouble)
            {
                return (double)longForDouble;
            }

            if (value is decimal decimalForDouble)
            {
                try
                {
                    return decimal.ToDouble(decimalForDouble);
                }
                catch (OverflowException ex)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.",
                        ex);
                }
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType == typeof(float))
        {
            if (value is string floatString && float.TryParse(floatString, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatParsed))
            {
                if (!float.IsNaN(floatParsed) && !float.IsInfinity(floatParsed))
                {
                    return floatParsed;
                }
            }

            if (value is int intForFloat)
            {
                return (float)intForFloat;
            }

            if (value is long longForFloat)
            {
                return (float)longForFloat;
            }

            if (value is double doubleForFloat && !double.IsNaN(doubleForFloat) && !double.IsInfinity(doubleForFloat))
            {
                if (doubleForFloat >= float.MinValue && doubleForFloat <= float.MaxValue)
                {
                    return (float)doubleForFloat;
                }
            }

            if (value is decimal decimalForFloat)
            {
                try
                {
                    return decimal.ToSingle(decimalForFloat);
                }
                catch (OverflowException ex)
                {
                    throw new InvalidOperationException(
                        $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.",
                        ex);
                }
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        if (effectiveType.IsEnum)
        {
            if (value is string enumString && Enum.TryParse(effectiveType, enumString, true, out var enumParsed))
            {
                return enumParsed;
            }

            if (value is int enumInt)
            {
                return Enum.ToObject(effectiveType, enumInt);
            }

            if (value is long enumLong)
            {
                return Enum.ToObject(effectiveType, enumLong);
            }

            throw new InvalidOperationException(
                $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
        }

        throw new InvalidOperationException(
            $"Type mismatch for parameter value '{value}': expected '{effectiveType.Name}' but received '{incomingType}'.");
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
                .Where(x => x.Attribute != null && !x.Method.IsSpecialName)
                .GroupBy(x => x.Attribute?.Name ?? x.Method.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        if (g.Count() > 1)
                        {
                            throw new InvalidOperationException($"Duplicate [DriverOperation] name '{g.Key}' on wrapper type '{type.FullName}'.");
                        }

                        return g.Single().Method;
                    },
                    StringComparer.OrdinalIgnoreCase);

            return methods;
        });
    }
}
