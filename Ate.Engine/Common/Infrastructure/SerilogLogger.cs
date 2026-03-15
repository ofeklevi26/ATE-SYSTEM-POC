using System;
using Serilog;

namespace Ate.Engine.Infrastructure;

public sealed class SerilogLogger : ILogger
{
    private readonly Serilog.ILogger _logger;

    public SerilogLogger(Serilog.ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Info(string message)
    {
        _logger.Information("{Message}", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        if (ex == null)
        {
            _logger.Error("{Message}", message);
            return;
        }

        _logger.Error(ex, "{Message}", message);
    }
}
