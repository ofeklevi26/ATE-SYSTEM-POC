using System;

namespace Ate.Engine;

public static class Program
{
    public static void Main(string[] args)
    {
        using var runtime = EngineRuntime.Start();

        runtime.Logger.Info($"ATE engine listening at {runtime.BaseAddress}");
        runtime.Logger.Info("Press ENTER to stop...");
        Console.ReadLine();

        runtime.Logger.Info("Stopping engine...");
    }
}
