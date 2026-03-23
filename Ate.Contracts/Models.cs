using System.Collections.Generic;

namespace Ate.Contracts;

public sealed class DeviceCommandRequest
{
    public string DeviceType { get; set; } = string.Empty;

    public string? DriverId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public string? ClientRequestId { get; set; }
}

public sealed class DeviceCommandResponse
{
    public string ServerCommandId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class EngineStatus
{
    public string State { get; set; } = "Stopped";

    public int QueueLength { get; set; }

    public string? CurrentCommand { get; set; }

    public string? LastError { get; set; }

    public List<string> LoadedDrivers { get; set; } = new List<string>();
}

public enum ParameterKind
{
    String,
    Integer,
    Number,
    Boolean
}

public enum NumberFormat
{
    Decimal,
    Float,
    Double
}

public sealed class CommandParameterDefinition
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ParameterKind Kind { get; set; } = ParameterKind.String;

    public NumberFormat? NumberFormat { get; set; }

    public bool Nullable { get; set; }

    public string? Default { get; set; }
}

public sealed class CommandOperationDefinition
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ReturnType { get; set; } = string.Empty;

    public List<CommandParameterDefinition> Parameters { get; set; } = new List<CommandParameterDefinition>();

    public override string ToString() => Name;
}

public sealed class DeviceCommandDefinition
{
    public string DeviceType { get; set; } = string.Empty;

    public string DriverId { get; set; } = "default";

    public string DriverClassName { get; set; } = string.Empty;

    public string DriverDisplayName { get; set; } = string.Empty;

    public string DriverDescription { get; set; } = string.Empty;

    public List<CommandParameterDefinition> DriverParameters { get; set; } = new List<CommandParameterDefinition>();

    public List<CommandOperationDefinition> Operations { get; set; } = new List<CommandOperationDefinition>();

    public override string ToString() => DeviceType;
}
