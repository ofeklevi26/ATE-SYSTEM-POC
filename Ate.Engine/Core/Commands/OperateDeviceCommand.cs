using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Commands;

public sealed class OperateDeviceCommand : IAteCommand
{
    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;

    public OperateDeviceCommand(
        string commandId,
        string? clientRequestId,
        string deviceType,
        string deviceName,
        string operation,
        Dictionary<string, object> parameters,
        DriverRegistry driverRegistry,
        ILogger logger)
    {
        CommandId = commandId;
        ClientRequestId = clientRequestId;
        DeviceType = deviceType;
        DeviceName = deviceName;
        Operation = operation;
        Parameters = parameters;
        _driverRegistry = driverRegistry;
        _logger = logger;
    }

    public string CommandId { get; }

    public string? ClientRequestId { get; }

    public string DeviceType { get; }

    public string DeviceName { get; }

    public string Operation { get; }

    public Dictionary<string, object> Parameters { get; }

    public string Name => $"{CommandId}:{DeviceType}[{DeviceName}].{Operation}";

    public async Task ExecuteAsync(CancellationToken token)
    {
        if (!_driverRegistry.TryResolve(DeviceType, DeviceName, out var driver) || driver == null)
        {
            throw new InvalidOperationException($"No configured device registered for deviceType '{DeviceType}' and deviceName '{DeviceName}'.");
        }

        _logger.Info($"Executing command {Name} (clientRequestId={ClientRequestId ?? "n/a"}).");
        var result = await driver.ExecuteAsync(Operation, Parameters, token).ConfigureAwait(false);
        _logger.Info($"Command {Name} completed. Result={result}");
    }
}
