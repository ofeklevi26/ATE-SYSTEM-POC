using System.Web.Http;
using Ate.Contracts;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/status")]
public sealed class StatusController : ApiController
{
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetStatus()
    {
        var invoker = EngineHostContext.CommandInvoker;
        var status = new EngineStatus
        {
            State = invoker.State,
            QueueLength = invoker.QueueLength,
            CurrentCommand = invoker.CurrentCommand,
            LastError = invoker.LastError,
            LoadedDrivers = new System.Collections.Generic.List<string>(EngineHostContext.DriverRegistry.GetLoadedDrivers())
        };

        return Ok(status);
    }
}
