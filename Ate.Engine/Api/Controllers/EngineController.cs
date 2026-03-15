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
        _commandInvoker.Pause();
        _logger.Info("Engine queue paused by API request.");
        return Ok();
    }

    [HttpPost]
    [Route("resume")]
    public IHttpActionResult Resume()
    {
        _commandInvoker.Resume();
        _logger.Info("Engine queue resumed by API request.");
        return Ok();
    }

    [HttpPost]
    [Route("clear")]
    public IHttpActionResult Clear()
    {
        _commandInvoker.ClearPending();
        _logger.Info("Engine queue cleared by API request.");
        return Ok();
    }

    [HttpPost]
    [Route("abort-current")]
    public IHttpActionResult AbortCurrent()
    {
        _commandInvoker.AbortCurrent();
        _logger.Info("Current command abort requested via API.");
        return Ok();
    }
}
