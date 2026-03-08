using System;
using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoDmmHardwareDriver : IDmmHardwareDriver
{
    public void Connect(string ip)
    {
        _ = ip;
    }

    public void Disconnect()
    {
    }

    public string Identify(string ip, int channel)
    {
        return $"DMM-DEMO@{ip}:CH{channel}";
    }

    public decimal MeasureVoltage(string ip, int channel, decimal range)
    {
        var seed = (ip.GetHashCode() & 0x7fffffff) % 100;
        return 3.0m + channel * 0.1m + (range > 10 ? 0.05m : 0m) + seed / 1000m;
    }
}
