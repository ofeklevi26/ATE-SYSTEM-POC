using System;
using System.IO;
using Serilog;

namespace Ate.Engine.Infrastructure;

public static class SerilogBootstrapper
{
    public static ILogger CreateLogger(string baseDirectory)
    {
        var logsDirectory = Path.Combine(baseDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logsDirectory, "engine-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Logger = serilogLogger;

        return new SerilogLogger(serilogLogger);
    }

    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}
