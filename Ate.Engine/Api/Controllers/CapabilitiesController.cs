using System.Linq;
using System.Web.Http;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/capabilities")]
public sealed class CapabilitiesController : ApiController
{
    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;

    public CapabilitiesController(DriverRegistry driverRegistry, ILogger logger)
    {
        _driverRegistry = driverRegistry;
        _logger = logger;
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult GetCapabilities()
    {
        var definitions = _driverRegistry.GetCommandDefinitions();
        var operationCount = definitions.Sum(d => d.Operations.Count);
        var summary = definitions.Count == 0
            ? "none"
            : string.Join(", ", definitions.Select(d => $"{d.DeviceType}/{d.DriverId} ({d.Operations.Count} ops)"));

        _logger.Info(
            $"Capabilities requested: devices={definitions.Count}, operations={operationCount}, definitions=[{summary}] (driverId source: set per device in engine-config.json; clients send that value in POST /api/command request.driverId. If omitted, engine resolves driverId='default' when available).");
        return Ok(definitions);
    }
}
