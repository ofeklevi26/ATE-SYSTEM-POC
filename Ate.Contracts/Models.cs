using System.Collections.Generic;

namespace Ate.Contracts;

public sealed class DeviceCommandRequest
{
    public string DeviceType { get; set; } = string.Empty;

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
