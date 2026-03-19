using System;

namespace Ate.Engine.Drivers;

public sealed class ParameterTypeMismatchException : InvalidOperationException
{
    public ParameterTypeMismatchException(
        string operation,
        string parameterName,
        string expectedType,
        object? receivedValue)
        : base(BuildMessage(operation, parameterName, expectedType, receivedValue))
    {
        Operation = operation;
        ParameterName = parameterName;
        ExpectedType = expectedType;
        ReceivedValue = receivedValue;
    }

    public string Operation { get; }

    public string ParameterName { get; }

    public string ExpectedType { get; }

    public object? ReceivedValue { get; }

    private static string BuildMessage(string operation, string parameterName, string expectedType, object? receivedValue)
    {
        var receivedType = receivedValue?.GetType().Name ?? "null";
        var receivedValueText = receivedValue == null ? "null" : $"'{receivedValue}'";
        return $"Type mismatch for parameter '{parameterName}' in operation '{operation}'. Expected '{expectedType}' but received {receivedType} ({receivedValueText}).";
    }
}
