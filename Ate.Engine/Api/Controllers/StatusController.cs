using System;
using System.Collections.Generic;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/status")]
public sealed class StatusController : ApiController
{
    private readonly CommandInvoker _commandInvoker;
    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;

    public StatusController(CommandInvoker commandInvoker, DriverRegistry driverRegistry, ILogger logger)
    {
        _commandInvoker = commandInvoker;
        _driverRegistry = driverRegistry;
        _logger = logger;
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult GetStatus()
    {
        try
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
        catch (Exception ex)
        {
            _logger.Error("Failed to retrieve engine status.", ex);
            return InternalServerError();
        }
    }
}
