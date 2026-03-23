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
                    DeviceName = "DMM",
                    DeviceType = "DMM",
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
                    DeviceName = "PSU",
                    DeviceType = "PSU",
                    Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["address"] = "192.168.0.20",
                        ["port"] = "5025",
                        ["channel"] = "1",
                        ["endpointFormat"] = "tcp-{address}:{port}"
                    }
                },
                new DriverInstanceConfiguration
                {
                    DeviceName = "PSU2",
                    DeviceType = "PSU",
                    Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["address"] = "192.168.0.21",
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
    public string DeviceName { get; set; } = string.Empty;

    public string DeviceType { get; set; } = string.Empty;

    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
