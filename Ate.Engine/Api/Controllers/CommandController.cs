using System;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;
using Ate.Engine.Serialization;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/command")]
public sealed class CommandController : ApiController
{
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
            EngineHostContext.DriverRegistry,
            EngineHostContext.Logger);

        EngineHostContext.CommandInvoker.Enqueue(command);

        return Ok(new DeviceCommandResponse
        {
            ServerCommandId = id,
            Message = "Command enqueued."
        });
    }
}
