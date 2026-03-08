using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ate.Engine.Commands;
using Ate.Engine.Configuration;
using Ate.Engine.DeviceIntegration.Providers;
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

        var driversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drivers");
        RegisterConfiguredDriverWrappers(config, registry, logger, driversPath);

        var loader = new DriverLoader(registry, logger);
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

    private static void RegisterConfiguredDriverWrappers(EngineConfiguration config, DriverRegistry registry, ILogger logger, string driversPath)
    {
        var providers = DiscoverConfiguredWrapperProviders(logger, driversPath);

        foreach (var cfg in config.Drivers)
        {
            var provider = ResolveProvider(providers, cfg);
            if (provider == null)
            {
                logger.Error($"No wrapper provider found for deviceType='{cfg.DeviceType}', wrapperProviderType='{cfg.WrapperProviderType ?? "(auto)"}'.");
                continue;
            }

            var registration = provider.Create(cfg, logger);
            registry.RegisterInstance(registration.Driver, registration.Definition);
            logger.Info($"Registered configured wrapper via provider '{provider.Name}': {registration.Description ?? cfg.DriverId}");
        }
    }

    private static IReadOnlyList<IConfiguredWrapperProvider> DiscoverConfiguredWrapperProviders(ILogger logger, string driversPath)
    {
        var providers = new List<IConfiguredWrapperProvider>
        {
            new DmmConfiguredWrapperProvider(),
            new PsuConfiguredWrapperProvider()
        };

        if (!Directory.Exists(driversPath))
        {
            return providers;
        }

        foreach (var dll in Directory.GetFiles(driversPath, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var type in asm.GetTypes().Where(IsConfiguredProviderType))
                {
                    var provider = (IConfiguredWrapperProvider)Activator.CreateInstance(type)!;
                    providers.Add(provider);
                    logger.Info($"Loaded configured wrapper provider '{type.FullName}' from '{dll}'.");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to discover configured wrapper providers from '{dll}'.", ex);
            }
        }

        return providers;
    }

    private static IConfiguredWrapperProvider? ResolveProvider(IReadOnlyList<IConfiguredWrapperProvider> providers, DriverInstanceConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.WrapperProviderType))
        {
            return providers.FirstOrDefault(p =>
                p.Name.Equals(configuration.WrapperProviderType, StringComparison.OrdinalIgnoreCase) ||
                p.GetType().FullName?.Equals(configuration.WrapperProviderType, StringComparison.OrdinalIgnoreCase) == true ||
                p.GetType().Name.Equals(configuration.WrapperProviderType, StringComparison.OrdinalIgnoreCase));
        }

        return providers.FirstOrDefault(p => p.CanCreate(configuration));
    }

    private static bool IsConfiguredProviderType(Type type)
    {
        return !type.IsAbstract && typeof(IConfiguredWrapperProvider).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
