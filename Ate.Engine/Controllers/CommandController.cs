using System;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;

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
            request.Operation,
            request.Parameters ?? new System.Collections.Generic.Dictionary<string, object>(),
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
