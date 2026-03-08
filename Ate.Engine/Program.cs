using System;
using System.IO;
using Ate.Engine.BuiltInDrivers;
using Ate.Engine.Commands;
using Ate.Engine.Configuration;
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

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine-config.json");
        var config = EngineConfiguration.Load(configPath);

        RegisterConfiguredDriverWrappers(config, registry, logger);

        var loader = new DriverLoader(registry, logger);
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

    private static void RegisterConfiguredDriverWrappers(EngineConfiguration config, DriverRegistry registry, ILogger logger)
    {
        foreach (var driver in config.Drivers)
        {
            if (driver.DeviceType.Equals("DMM", StringComparison.OrdinalIgnoreCase))
            {
                registry.RegisterInstance(new DmmDemoDriver(driver.DriverId, driver.Ip, driver.Channel));
                logger.Info($"Registered configured wrapper DMM::{driver.DriverId} @ {driver.Ip} CH{driver.Channel}");
                continue;
            }

            if (driver.DeviceType.Equals("PSU", StringComparison.OrdinalIgnoreCase))
            {
                registry.RegisterInstance(new PsuDemoDriver(driver.DriverId, driver.Ip, driver.Channel));
                logger.Info($"Registered configured wrapper PSU::{driver.DriverId} @ {driver.Ip} CH{driver.Channel}");
                continue;
            }

            logger.Error($"Unsupported configured device type '{driver.DeviceType}'.");
        }
    }
}
