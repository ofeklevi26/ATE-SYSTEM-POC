using System;

namespace Ate.Engine.DemoDevices;

public sealed class DmmDemoDevice
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
        return $"DMM-DEMO@{ip}:CH{channel}";
    }

    public decimal MeasureVoltage(string ip, int channel, decimal range)
    {
        EnsureConnected();
        var seed = (ip.GetHashCode() & 0x7fffffff) % 100;
        return 3.0m + channel * 0.1m + (range > 10 ? 0.05m : 0m) + seed / 1000m;
    }

    private void EnsureConnected()
    {
        if (!_connected)
        {
            throw new InvalidOperationException("Device not connected.");
        }
    }
}
