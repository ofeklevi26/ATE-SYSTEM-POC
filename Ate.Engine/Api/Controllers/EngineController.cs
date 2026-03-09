using System.Web.Http;
using Ate.Engine.Commands;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/engine")]
public sealed class EngineController : ApiController
{
    private readonly CommandInvoker _commandInvoker;

    public EngineController(CommandInvoker commandInvoker)
    {
        _commandInvoker = commandInvoker;
    }

    [HttpPost]
    [Route("pause")]
    public IHttpActionResult Pause()
    {
        _commandInvoker.Pause();
        return Ok();
    }

    [HttpPost]
    [Route("resume")]
    public IHttpActionResult Resume()
    {
        _commandInvoker.Resume();
        return Ok();
    }

    [HttpPost]
    [Route("clear")]
    public IHttpActionResult Clear()
    {
        _commandInvoker.ClearPending();
        return Ok();
    }

    [HttpPost]
    [Route("abort-current")]
    public IHttpActionResult AbortCurrent()
    {
        _commandInvoker.AbortCurrent();
        return Ok();
    }
}
