using System;
using System.Web.Http;
using Ate.Engine.Commands;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/engine")]
public sealed class EngineController : ApiController
{
    private readonly CommandInvoker _commandInvoker;
    private readonly ILogger _logger;

    public EngineController(CommandInvoker commandInvoker, ILogger logger)
    {
        _commandInvoker = commandInvoker;
        _logger = logger;
    }

    [HttpPost]
    [Route("pause")]
    public IHttpActionResult Pause()
    {
        try
        {
            _commandInvoker.Pause();
            _logger.Info("Engine queue paused by API request.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to pause engine queue.", ex);
            return InternalServerError();
        }
    }

    [HttpPost]
    [Route("resume")]
    public IHttpActionResult Resume()
    {
        try
        {
            _commandInvoker.Resume();
            _logger.Info("Engine queue resumed by API request.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to resume engine queue.", ex);
            return InternalServerError();
        }
    }

    [HttpPost]
    [Route("clear")]
    public IHttpActionResult Clear()
    {
        try
        {
            _commandInvoker.ClearPending();
            _logger.Info("Engine queue cleared by API request.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to clear engine queue.", ex);
            return InternalServerError();
        }
    }

    [HttpPost]
    [Route("abort-current")]
    public IHttpActionResult AbortCurrent()
    {
        try
        {
            _commandInvoker.AbortCurrent();
            _logger.Info("Current command abort requested via API.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to abort current command.", ex);
            return InternalServerError();
        }
    }
}
