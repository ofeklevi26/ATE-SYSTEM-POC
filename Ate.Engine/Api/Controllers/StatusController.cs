using System.Collections.Generic;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;
using Ate.Engine.Drivers;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/status")]
public sealed class StatusController : ApiController
{
    private readonly CommandInvoker _commandInvoker;
    private readonly DriverRegistry _driverRegistry;

    public StatusController(CommandInvoker commandInvoker, DriverRegistry driverRegistry)
    {
        _commandInvoker = commandInvoker;
        _driverRegistry = driverRegistry;
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult GetStatus()
    {
        var status = new EngineStatus
        {
            State = _commandInvoker.State,
            QueueLength = _commandInvoker.QueueLength,
            CurrentCommand = _commandInvoker.CurrentCommand,
            LastError = _commandInvoker.LastError,
            LoadedDrivers = new List<string>(_driverRegistry.GetLoadedDrivers())
        };

        return Ok(status);
    }
}
