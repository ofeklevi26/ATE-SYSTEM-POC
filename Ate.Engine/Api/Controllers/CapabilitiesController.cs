using System.Web.Http;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/capabilities")]
public sealed class CapabilitiesController : ApiController
{
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetCapabilities()
    {
        var definitions = EngineHostContext.DriverRegistry.GetCommandDefinitions();
        return Ok(definitions);
    }
}
