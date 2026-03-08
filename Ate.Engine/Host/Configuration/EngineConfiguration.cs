using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Ate.Engine.Configuration;

public sealed class EngineConfiguration
{
    public List<DriverInstanceConfiguration> Drivers { get; set; } = new List<DriverInstanceConfiguration>();

    public static EngineConfiguration Load(string path)
    {
        if (!File.Exists(path))
        {
            return CreateDefault();
        }

        try
        {
            var raw = File.ReadAllText(path);
            var cfg = JsonConvert.DeserializeObject<EngineConfiguration>(raw);
            return cfg ?? CreateDefault();
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static EngineConfiguration CreateDefault()
    {
        return new EngineConfiguration
        {
            Drivers = new List<DriverInstanceConfiguration>
            {
                new DriverInstanceConfiguration { DeviceType = "DMM", DriverId = "default", Ip = "192.168.0.10", Channel = 1 },
                new DriverInstanceConfiguration { DeviceType = "PSU", DriverId = "default", Ip = "192.168.0.20", Channel = 1 }
            }
        };
    }
}

public sealed class DriverInstanceConfiguration
{
    public string DeviceType { get; set; } = string.Empty;

    public string DriverId { get; set; } = "default";

    public string Ip { get; set; } = string.Empty;

    public int Channel { get; set; } = 1;
}
