using System;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;
using Ate.Engine.Serialization;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/command")]
public sealed class CommandController : ApiController
{
    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;
    private readonly CommandInvoker _commandInvoker;

    public CommandController(DriverRegistry driverRegistry, ILogger logger, CommandInvoker commandInvoker)
    {
        _driverRegistry = driverRegistry;
        _logger = logger;
        _commandInvoker = commandInvoker;
    }

    [HttpPost]
    [Route("")]
    public IHttpActionResult EnqueueCommand([FromBody] DeviceCommandRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceType) || string.IsNullOrWhiteSpace(request.Operation))
        {
            _logger.Error("Rejected command request because DeviceType or Operation is missing.");
            return BadRequest("DeviceType and Operation are required.");
        }

        var id = Guid.NewGuid().ToString("N");
        var command = new OperateDeviceCommand(
            id,
            request.ClientRequestId,
            request.DeviceType,
            request.DriverId,
            request.Operation,
            ParameterValueNormalizer.Normalize(request.Parameters),
            _driverRegistry,
            _logger);

        _commandInvoker.Enqueue(command);
        _logger.Info($"Command enqueued: {request.DeviceType}/{ResolveDriverIdForLog(request.DriverId)}::{request.Operation} (serverCommandId={id}).");

        return Ok(new DeviceCommandResponse
        {
            ServerCommandId = id,
            Message = "Command enqueued."
        });
    }

    private static string ResolveDriverIdForLog(string? driverId)
    {
        return string.IsNullOrWhiteSpace(driverId) ? "default" : driverId;
    }
}
