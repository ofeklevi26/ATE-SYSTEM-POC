using System.Web.Http;
using Ate.Engine.Drivers;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/capabilities")]
public sealed class CapabilitiesController : ApiController
{
    private readonly DriverRegistry _driverRegistry;

    public CapabilitiesController(DriverRegistry driverRegistry)
    {
        _driverRegistry = driverRegistry;
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult GetCapabilities()
    {
        var definitions = _driverRegistry.GetCommandDefinitions();
        return Ok(definitions);
    }
}
