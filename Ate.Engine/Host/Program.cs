using System;
using Ate.Engine.Infrastructure;

namespace Ate.Engine;

public static class Program
{
    public static void Main(string[] args)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        ILogger? emergencyLogger = null;

        try
        {
            emergencyLogger = SerilogBootstrapper.CreateLogger(baseDirectory);
            emergencyLogger.Info("Starting ATE engine runtime.");

            using var runtime = EngineRuntime.Start(emergencyLogger);

            runtime.Logger.Info($"ATE engine listening at {runtime.BaseAddress}");
            runtime.Logger.Info("Press ENTER to stop...");
            Console.ReadLine();

            runtime.Logger.Info("Stopping engine...");
        }
        catch (Exception ex)
        {
            emergencyLogger?.Error("Unhandled fatal exception while running ATE engine.", ex);
            throw;
        }
        finally
        {
            SerilogBootstrapper.Shutdown();
        }
    }
}
