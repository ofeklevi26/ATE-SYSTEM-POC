using System;
using System.Web.Http;
using Ate.Contracts;
using Ate.Engine.Commands;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;
using Ate.Engine.Serialization;

namespace Ate.Engine.Controllers;

[RoutePrefix("api/command")]
public sealed class CommandController : ApiController
{
    private const string GenericCommandValidationError = "Command validation failed. Check the engine log window for details.";

    private readonly DriverRegistry _driverRegistry;
    private readonly ILogger _logger;
    private readonly CommandInvoker _commandInvoker;

    public CommandController(DriverRegistry driverRegistry, ILogger logger, CommandInvoker commandInvoker)
    {
        _driverRegistry = driverRegistry;
        _logger = logger;
        _commandInvoker = commandInvoker;
    }

    [HttpPost]
    [Route("")]
    public IHttpActionResult EnqueueCommand([FromBody] DeviceCommandRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DeviceType) || string.IsNullOrWhiteSpace(request.Operation))
            {
                _logger.Error("Rejected command request because DeviceType or Operation is missing.");
                return BadRequest("DeviceType and Operation are required.");
            }

            var id = Guid.NewGuid().ToString("N");
            var normalizedParameters = ParameterValueNormalizer.Normalize(request.Parameters);

            if (!_driverRegistry.TryResolve(request.DeviceType, request.DriverId, out var driver) || driver == null)
            {
                var driverResolutionError =
                    $"No driver registered for device '{request.DeviceType}' and driverId '{ResolveDriverIdForLog(request.DriverId)}'.";
                _commandInvoker.ReportError(driverResolutionError);
                return BadRequest(GenericCommandValidationError);
            }

            try
            {
                WrapperOperationRuntime.ValidateInvocation(driver, request.Operation, normalizedParameters);
            }
            catch (ParameterTypeMismatchException ex)
            {
                _commandInvoker.ReportError($"Rejected command due to type mismatch: {ex.Message}");
                return BadRequest(GenericCommandValidationError);
            }
            catch (InvalidOperationException ex)
            {
                _commandInvoker.ReportError($"Rejected command due to invalid invocation: {ex.Message}");
                return BadRequest(GenericCommandValidationError);
            }

            var command = new OperateDeviceCommand(
                id,
                request.ClientRequestId,
                request.DeviceType,
                request.DriverId,
                request.Operation,
                normalizedParameters,
                _driverRegistry,
                _logger);

            _commandInvoker.Enqueue(command);
            _logger.Info($"Command enqueued: {request.DeviceType}/{ResolveDriverIdForLog(request.DriverId)}::{request.Operation} (serverCommandId={id}).");

            return Ok(new DeviceCommandResponse
            {
                ServerCommandId = id,
                Message = "Command enqueued."
            });
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to enqueue command request.", ex);
            return InternalServerError();
        }
    }

    private static string ResolveDriverIdForLog(string? driverId)
    {
        return string.IsNullOrWhiteSpace(driverId) ? "default" : driverId;
    }
}
