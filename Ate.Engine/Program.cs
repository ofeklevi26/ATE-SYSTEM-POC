using System;
using System.Collections.Generic;
using System.IO;
using Ate.Contracts;
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
        foreach (var cfg in config.Drivers)
        {
            if (cfg.DeviceType.Equals("DMM", StringComparison.OrdinalIgnoreCase))
            {
                var wrapper = new DmmDemoDriver(cfg.DriverId, cfg.Ip, cfg.Channel);
                registry.RegisterInstance(wrapper, BuildDefinitionForDmm(cfg));
                logger.Info($"Registered configured wrapper DMM::{cfg.DriverId} @ {cfg.Ip} CH{cfg.Channel}");
                continue;
            }

            if (cfg.DeviceType.Equals("PSU", StringComparison.OrdinalIgnoreCase))
            {
                var wrapper = new PsuDemoDriver(cfg.DriverId, cfg.Ip, cfg.Channel);
                registry.RegisterInstance(wrapper, BuildDefinitionForPsu(cfg));
                logger.Info($"Registered configured wrapper PSU::{cfg.DriverId} @ {cfg.Ip} CH{cfg.Channel}");
                continue;
            }

            logger.Error($"Unsupported configured device type '{cfg.DeviceType}'.");
        }
    }

    private static DeviceCommandDefinition BuildDefinitionForDmm(DriverInstanceConfiguration cfg)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = cfg.DeviceType,
            DriverId = cfg.DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition
                {
                    Name = "channel",
                    Type = ParameterValueType.Integer,
                    IsRequired = true,
                    DefaultValue = cfg.Channel.ToString()
                }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "MeasureVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "range", Type = ParameterValueType.Decimal, DefaultValue = "10.0" }
                    }
                },
                new CommandOperationDefinition { Name = "Identify" }
            }
        };
    }

    private static DeviceCommandDefinition BuildDefinitionForPsu(DriverInstanceConfiguration cfg)
    {
        return new DeviceCommandDefinition
        {
            DeviceType = cfg.DeviceType,
            DriverId = cfg.DriverId,
            DriverParameters = new List<CommandParameterDefinition>
            {
                new CommandParameterDefinition
                {
                    Name = "channel",
                    Type = ParameterValueType.Integer,
                    IsRequired = true,
                    DefaultValue = cfg.Channel.ToString()
                }
            },
            Operations = new List<CommandOperationDefinition>
            {
                new CommandOperationDefinition
                {
                    Name = "SetVoltage",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "voltage", Type = ParameterValueType.Decimal, IsRequired = true, DefaultValue = "5.0" },
                        new CommandParameterDefinition { Name = "currentLimit", Type = ParameterValueType.Decimal, DefaultValue = "1.0" }
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetCurrentLimit",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "currentLimit", Type = ParameterValueType.Decimal, IsRequired = true, DefaultValue = "1.0" }
                    }
                },
                new CommandOperationDefinition
                {
                    Name = "SetOutput",
                    Parameters = new List<CommandParameterDefinition>
                    {
                        new CommandParameterDefinition { Name = "enabled", Type = ParameterValueType.Boolean, DefaultValue = "true" }
                    }
                },
                new CommandOperationDefinition { Name = "OutputOff" },
                new CommandOperationDefinition { Name = "Identify" }
            }
        };
    }
}
