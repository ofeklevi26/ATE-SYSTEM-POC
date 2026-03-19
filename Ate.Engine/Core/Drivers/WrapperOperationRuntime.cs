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
                return ConvertValue(raw, param.ParameterType);
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
