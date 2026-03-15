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

    public static EngineRuntime Start(ILogger? bootLoggerOverride = null)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var ownsLogger = bootLoggerOverride == null;
        var bootLogger = bootLoggerOverride ?? SerilogBootstrapper.CreateLogger(baseDirectory);

        try
        {
            var driversPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "drivers");
            var pluginAssemblies = DiscoverDriverAssemblies(driversPath, bootLogger);

            var services = BuildServiceCollection(pluginAssemblies, bootLogger);
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger>();
            var registry = serviceProvider.GetRequiredService<DriverRegistry>();
            var invoker = serviceProvider.GetRequiredService<CommandInvoker>();
            var configuredWrapperRegistrar = serviceProvider.GetRequiredService<ConfiguredWrapperRegistrar>();

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine-config.json");
            var config = EngineConfiguration.Load(configPath);

            configuredWrapperRegistrar.Register(config);

            var loader = new DriverLoader(registry, logger);
            loader.LoadFromAssemblies(pluginAssemblies);

            invoker.Start();

            var dependencyResolver = new ServiceProviderDependencyResolver(serviceProvider);
            const string baseAddress = "http://localhost:9000/";
            var webApp = WebApp.Start(baseAddress, appBuilder => new Startup(dependencyResolver, logger).Configuration(appBuilder));

            return new EngineRuntime(baseAddress, logger, invoker, webApp);
        }
        catch (Exception ex)
        {
            bootLogger.Error("Engine runtime failed during startup.", ex);

            if (ownsLogger)
            {
                SerilogBootstrapper.Shutdown();
            }

            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _webApp.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed while disposing HTTP web host.", ex);
        }

        try
        {
            _invoker.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed while stopping command invoker.", ex);
        }
    }

    private static ServiceCollection BuildServiceCollection(IReadOnlyList<Assembly> pluginAssemblies, ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(logger);
        services.AddSingleton<DriverRegistry>();
        services.AddSingleton<CommandInvoker>();
        services.AddSingleton<ConfiguredWrapperRegistrar>();

        foreach (var module in DiscoverDriverModules(pluginAssemblies, logger))
        {
            module.Register(services);
        }

        services.AddTransient<CommandController>();
        services.AddTransient<StatusController>();
        services.AddTransient<EngineController>();
        services.AddTransient<CapabilitiesController>();

        return services;
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

    private static IReadOnlyList<IDriverModule> DiscoverDriverModules(IReadOnlyList<Assembly> pluginAssemblies, ILogger logger)
    {
        var modules = new List<IDriverModule>();
        var assemblies = new List<Assembly> { typeof(EngineRuntime).Assembly };
        assemblies.AddRange(pluginAssemblies);

        foreach (var assembly in assemblies.Distinct())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.Error($"Failed to reflect all types from assembly '{assembly.FullName}'.", ex);
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to inspect assembly '{assembly.FullName}' for driver modules.", ex);
                continue;
            }

            foreach (var type in types.Where(IsDriverModuleType))
            {
                try
                {
                    if (Activator.CreateInstance(type) is IDriverModule module)
                    {
                        modules.Add(module);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to instantiate driver module '{type.FullName}'.", ex);
                }
            }
        }

        return modules;
    }

    private static bool IsDriverModuleType(Type type)
    {
        return !type.IsAbstract && typeof(IDriverModule).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
