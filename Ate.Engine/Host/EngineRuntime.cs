using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ate.Engine.Commands;
using Ate.Engine.Configuration;
using Ate.Engine.Controllers;
using Ate.Engine.DemoDrivers;
using Ate.Engine.DeviceIntegration.Providers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;

namespace Ate.Engine;

public sealed class EngineRuntime : IDisposable
{
    private readonly IDisposable _webApp;
    private readonly CommandInvoker _invoker;

    private EngineRuntime(string baseAddress, ILogger logger, CommandInvoker invoker, IDisposable webApp)
    {
        BaseAddress = baseAddress;
        Logger = logger;
        _invoker = invoker;
        _webApp = webApp;
    }

    public string BaseAddress { get; }

    public ILogger Logger { get; }

    public static EngineRuntime Start()
    {
        var services = BuildServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var registry = serviceProvider.GetRequiredService<DriverRegistry>();
        var invoker = serviceProvider.GetRequiredService<CommandInvoker>();

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine-config.json");
        var config = EngineConfiguration.Load(configPath);

        var driversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drivers");
        RegisterConfiguredDriverWrappers(config, registry, logger, driversPath, serviceProvider);

        var loader = new DriverLoader(registry, logger);
        loader.LoadFromDirectory(driversPath);

        invoker.Start();

        var dependencyResolver = new ServiceProviderDependencyResolver(serviceProvider);
        const string baseAddress = "http://localhost:9000/";
        var webApp = WebApp.Start(baseAddress, appBuilder => new Startup(dependencyResolver).Configuration(appBuilder));

        return new EngineRuntime(baseAddress, logger, invoker, webApp);
    }

    public void Dispose()
    {
        _webApp.Dispose();
        _invoker.StopAsync().GetAwaiter().GetResult();
    }

    private static ServiceCollection BuildServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<DriverRegistry>();
        services.AddSingleton<CommandInvoker>();

        services.AddSingleton<IDmmHardwareDriverFactory, DemoDmmHardwareDriverFactory>();
        services.AddSingleton<IPsuHardwareDriverFactory, DemoPsuHardwareDriverFactory>();

        services.AddSingleton<IConfiguredWrapperProvider, DmmConfiguredWrapperProvider>();
        services.AddSingleton<IConfiguredWrapperProvider, PsuConfiguredWrapperProvider>();

        services.AddTransient<CommandController>();
        services.AddTransient<StatusController>();
        services.AddTransient<EngineController>();
        services.AddTransient<CapabilitiesController>();

        return services;
    }

    private static void RegisterConfiguredDriverWrappers(
        EngineConfiguration config,
        DriverRegistry registry,
        ILogger logger,
        string driversPath,
        IServiceProvider serviceProvider)
    {
        var providers = DiscoverConfiguredWrapperProviders(logger, driversPath, serviceProvider);

        foreach (var cfg in config.Drivers)
        {
            var provider = ResolveProvider(providers, cfg);
            if (provider == null)
            {
                logger.Error($"No wrapper provider found for deviceType='{cfg.DeviceType}', wrapperProviderType='{cfg.WrapperProviderType ?? "(auto)"}'.");
                continue;
            }

            try
            {
                var registration = provider.Create(cfg, logger);
                registry.RegisterInstance(registration.Driver, registration.Definition);
                logger.Info($"Registered configured wrapper via provider '{provider.Name}': {registration.Description ?? cfg.DriverId}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create configured wrapper via provider '{provider.Name}' for driverId='{cfg.DriverId}'.", ex);
            }
        }
    }

    private static IReadOnlyList<IConfiguredWrapperProvider> DiscoverConfiguredWrapperProviders(
        ILogger logger,
        string driversPath,
        IServiceProvider serviceProvider)
    {
        var providers = serviceProvider.GetServices<IConfiguredWrapperProvider>().ToList();

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
                    var provider = (IConfiguredWrapperProvider)ActivatorUtilities.CreateInstance(serviceProvider, type);
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
        return !type.IsAbstract && typeof(IConfiguredWrapperProvider).IsAssignableFrom(type);
    }
}
