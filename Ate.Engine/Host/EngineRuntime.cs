using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ate.Engine.Commands;
using Ate.Engine.Configuration;
using Ate.Engine.Controllers;
using Ate.Engine.Drivers;
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
        var bootLogger = new ConsoleLogger();
        var driversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drivers");
        var pluginAssemblies = DiscoverDriverAssemblies(driversPath, bootLogger);

        var services = BuildServiceCollection(pluginAssemblies);
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var registry = serviceProvider.GetRequiredService<DriverRegistry>();
        var invoker = serviceProvider.GetRequiredService<CommandInvoker>();

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine-config.json");
        var config = EngineConfiguration.Load(configPath);

        RegisterConfiguredDriverWrappers(config, registry, logger, serviceProvider);

        var loader = new DriverLoader(registry, logger);
        loader.LoadFromAssemblies(pluginAssemblies);

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

    private static ServiceCollection BuildServiceCollection(IReadOnlyList<Assembly> pluginAssemblies)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<DriverRegistry>();
        services.AddSingleton<CommandInvoker>();

        foreach (var module in DiscoverDriverModules(pluginAssemblies))
        {
            module.Register(services);
        }

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
        var descriptors = serviceProvider.GetServices<ConfiguredWrapperDescriptor>().ToList();

        foreach (var cfg in config.Drivers)
        {
            var descriptor = ResolveDescriptor(descriptors, cfg);
            if (descriptor == null)
            {
                logger.Error($"No wrapper descriptor found for deviceType='{cfg.DeviceType}', wrapperType='{cfg.WrapperType ?? "(auto)"}'.");
                continue;
            }

            try
            {
                var wrapper = ConfiguredWrapperFactory.Create(cfg, descriptor.WrapperType, serviceProvider);
                registry.RegisterInstance(wrapper, WrapperOperationRuntime.BuildDefinition(wrapper));
                logger.Info($"Registered configured wrapper '{descriptor.WrapperType.Name}' for {cfg.DeviceType}::{cfg.DriverId}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create configured wrapper '{descriptor.WrapperType.FullName}' for driverId='{cfg.DriverId}'.", ex);
            }
        }
    }

    private static IReadOnlyList<Assembly> DiscoverDriverAssemblies(string driversPath, ILogger logger)
    {
        var assemblies = new List<Assembly>();

        if (!Directory.Exists(driversPath))
        {
            return assemblies;
        }

        foreach (var dll in Directory.GetFiles(driversPath, "*.dll"))
        {
            try
            {
                assemblies.Add(Assembly.LoadFrom(dll));
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load driver assembly '{dll}'.", ex);
            }
        }

        return assemblies;
    }

    private static IReadOnlyList<IDriverModule> DiscoverDriverModules(IReadOnlyList<Assembly> pluginAssemblies)
    {
        var modules = new List<IDriverModule>();
        var assemblies = new List<Assembly> { typeof(EngineRuntime).Assembly };
        assemblies.AddRange(pluginAssemblies);

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in assembly.GetTypes().Where(IsDriverModuleType))
            {
                if (Activator.CreateInstance(type) is IDriverModule module)
                {
                    modules.Add(module);
                }
            }
        }

        return modules;
    }

    private static ConfiguredWrapperDescriptor? ResolveDescriptor(IReadOnlyList<ConfiguredWrapperDescriptor> descriptors, DriverInstanceConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.WrapperType))
        {
            return descriptors.FirstOrDefault(d =>
                d.DeviceType.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase) ||
                d.WrapperType.FullName?.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase) == true ||
                d.WrapperType.Name.Equals(configuration.WrapperType, StringComparison.OrdinalIgnoreCase));
        }

        return descriptors.FirstOrDefault(d => d.DeviceType.Equals(configuration.DeviceType, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDriverModuleType(Type type)
    {
        return !type.IsAbstract && typeof(IDriverModule).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
