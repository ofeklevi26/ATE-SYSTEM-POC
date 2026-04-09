using System;
using Ate.Engine.Hardware;
using Ate.Engine.Infrastructure;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoPsuHardwareDriverBuilder : IPsuDriverBuilder
{
    public IPsuDriverAdapter BuildPsuDriverAdapter(string deviceName, ILogger? logger = null)
    {
        return new DemoPsuHardwareDriverAdapter(deviceName, logger);
    }
}

public sealed class DemoPsuHardwareDriverAdapter : IPsuDriverAdapter
{
    private readonly string _deviceName;
    private readonly ILogger? _logger;
    private decimal _currentVoltage;
    private decimal _currentLimit;
    private bool _outputEnabled;
    private bool _connected;

    public DemoPsuHardwareDriverAdapter(string deviceName, ILogger? logger = null)
    {
        _deviceName = deviceName;
        _logger = logger;
    }

    public void Connect()
    {
        _connected = true;
        _logger?.Info($"PSU adapter connected to {_deviceName}.");
    }

    public void Disconnect()
    {
        _connected = false;
    }

    public string Identify(string address, int channel)
    {
        RequireConnection();
        return $"PSU-DEMO@{address}:CH{channel}";
    }

    public void SetVoltage(int channel, decimal voltage, decimal currentLimit)
    {
        RequireConnection();
        _ = channel;
        _currentVoltage = voltage;
        _currentLimit = currentLimit;
    }

    public void SetCurrentLimit(int channel, decimal currentLimit)
    {
        RequireConnection();
        _ = channel;
        _currentLimit = currentLimit;
    }

    public void SetOutput(int channel, bool enabled)
    {
        RequireConnection();
        _ = channel;
        _outputEnabled = enabled;
    }

    private void RequireConnection()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("No PSU adapter connection is active. Build an adapter and call Connect first.");
        }
    }
}
