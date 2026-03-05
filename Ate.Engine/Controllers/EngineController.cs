using System.Web.Http;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/engine")]
public sealed class EngineController : ApiController
{
    [HttpPost]
    [Route("pause")]
    public IHttpActionResult Pause()
    {
        EngineHostContext.CommandInvoker.Pause();
        return Ok();
    }

    [HttpPost]
    [Route("resume")]
    public IHttpActionResult Resume()
    {
        EngineHostContext.CommandInvoker.Resume();
        return Ok();
    }

    [HttpPost]
    [Route("clear")]
    public IHttpActionResult Clear()
    {
        EngineHostContext.CommandInvoker.ClearPending();
        return Ok();
    }

    [HttpPost]
    [Route("abort-current")]
    public IHttpActionResult AbortCurrent()
    {
        EngineHostContext.CommandInvoker.AbortCurrent();
        return Ok();
    }
}
