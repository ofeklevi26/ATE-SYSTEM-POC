using System;
using System.IO;
using Ate.Engine.BuiltInDrivers;
using Ate.Engine.Commands;
using Ate.Engine.Drivers;
using Ate.Engine.Infrastructure;
using Microsoft.Owin.Hosting;

namespace Ate.Engine;

public static class Program
{
    public static void Main(string[] args)
    {
        var logger = new ConsoleLogger();
        var registry = new DriverRegistry();
        var invoker = new CommandInvoker(logger);

        EngineHostContext.Logger = logger;
        EngineHostContext.DriverRegistry = registry;
        EngineHostContext.CommandInvoker = invoker;

        var loader = new DriverLoader(registry, logger);
        loader.LoadBuiltIns(typeof(DmmDemoDriver), typeof(PsuDemoDriver));

        var driversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drivers");
        loader.LoadFromDirectory(driversPath);

        invoker.Start();

        var baseAddress = "http://localhost:9000/";
        using (WebApp.Start<Startup>(url: baseAddress))
        {
            logger.Info($"ATE engine listening at {baseAddress}");
            logger.Info("Press ENTER to stop...");
            Console.ReadLine();
        }

        logger.Info("Stopping engine...");
        invoker.StopAsync().GetAwaiter().GetResult();
    }
}
