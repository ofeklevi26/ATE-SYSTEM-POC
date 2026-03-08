using System;

namespace Ate.Engine.DemoDevices;

public sealed class PsuDemoDevice
{
    private bool _connected;

    public void Connect(string ip)
    {
        _connected = true;
        _ = ip;
    }

    public void Disconnect()
    {
        _connected = false;
    }

    public string Identify(string ip, int channel)
    {
        EnsureConnected();
        return $"PSU-DEMO@{ip}:CH{channel}";
    }

    public void SetVoltage(int channel, decimal voltage, decimal currentLimit)
    {
        EnsureConnected();
        _ = channel;
        _ = voltage;
        _ = currentLimit;
    }

    public void SetCurrentLimit(int channel, decimal currentLimit)
    {
        EnsureConnected();
        _ = channel;
        _ = currentLimit;
    }

    public void SetOutput(int channel, bool enabled)
    {
        EnsureConnected();
        _ = channel;
        _ = enabled;
    }

    private void EnsureConnected()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Device not connected.");
        }
    }
}
