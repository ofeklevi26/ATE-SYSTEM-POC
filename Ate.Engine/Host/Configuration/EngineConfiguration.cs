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
            throw new FileNotFoundException($"Engine configuration file was not found at '{path}'.", path);
        }

        try
        {
            var raw = File.ReadAllText(path);
            var cfg = JsonConvert.DeserializeObject<EngineConfiguration>(raw);
            if (cfg == null)
            {
                throw new InvalidOperationException($"Engine configuration file '{path}' produced an empty configuration.");
            }

            return cfg;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Engine configuration file '{path}' contains invalid JSON.", ex);
        }
    }
}

public sealed class DriverInstanceConfiguration
{
    public string DeviceName { get; set; } = string.Empty;

    public string DeviceType { get; set; } = string.Empty;

    public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
