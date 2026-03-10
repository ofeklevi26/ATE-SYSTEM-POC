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
                new DriverInstanceConfiguration
                {
                    DeviceType = "DMM",
                    DriverId = "default",
                    Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["address"] = "192.168.0.10",
                        ["port"] = "5025",
                        ["channel"] = "1",
                        ["endpointFormat"] = "{address}:{port}"
                    }
                },
                new DriverInstanceConfiguration
                {
                    DeviceType = "PSU",
                    DriverId = "default",
                    Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["address"] = "192.168.0.20",
                        ["port"] = "5025",
                        ["channel"] = "1",
                        ["endpointFormat"] = "tcp-{address}:{port}"
                    }
                }
            }
        };
    }
}

public sealed class DriverInstanceConfiguration
{
    public string DeviceType { get; set; } = string.Empty;

    public string DriverId { get; set; } = "default";

    public string? WrapperType { get; set; }

    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
