using System;
using System.Web.Http.ExceptionHandling;
using Ate.Engine.Infrastructure;

namespace Ate.Engine;

public sealed class ApiExceptionLogger : ExceptionLogger
{
    private readonly ILogger _logger;

    public ApiExceptionLogger(ILogger logger)
    {
        _logger = logger;
    }

    public override void Log(ExceptionLoggerContext context)
    {
        if (context?.Exception == null)
        {
            _logger.Error("Unhandled API exception occurred without an exception payload.");
            return;
        }

        var requestUri = context.Request?.RequestUri?.ToString() ?? "unknown";
        var method = context.Request?.Method?.Method ?? "unknown";
        var controller = context.ExceptionContext?.ControllerContext?.ControllerDescriptor?.ControllerName ?? "unknown";
        var action = context.ExceptionContext?.ActionContext?.ActionDescriptor?.ActionName ?? "unknown";

        _logger.Error($"Unhandled API exception in {controller}.{action} for {method} {requestUri}.", context.Exception);
    }
}
