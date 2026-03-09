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
    private readonly CommandInvoker _commandInvoker;
    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;

    public CommandController(CommandInvoker commandInvoker, DriverRegistry driverRegistry, ILogger logger)
    {
        _commandInvoker = commandInvoker;
        _driverRegistry = driverRegistry;
        _logger = logger;
    }

    [HttpPost]
    [Route("")]
    public IHttpActionResult EnqueueCommand([FromBody] DeviceCommandRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceType) || string.IsNullOrWhiteSpace(request.Operation))
        {
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

        return Ok(new DeviceCommandResponse
        {
            ServerCommandId = id,
            Message = "Command enqueued."
        });
    }
}
