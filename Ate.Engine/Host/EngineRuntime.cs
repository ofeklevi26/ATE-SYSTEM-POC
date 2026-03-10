using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        RegisterConfiguredDriverWrappers(config, registry, logger, serviceProvider);

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
        IServiceProvider serviceProvider)
    {
        var providers = DiscoverConfiguredWrapperProviders(logger, serviceProvider);

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

    private static IReadOnlyList<IConfiguredWrapperProvider> DiscoverConfiguredWrapperProviders(
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        logger.Info("Runtime DLL discovery is disabled. Configure providers in host startup and engine-config.json.");

        return serviceProvider.GetServices<IConfiguredWrapperProvider>().ToList();
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
}
