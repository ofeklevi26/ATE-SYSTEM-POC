using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoDmmHardwareDriver : IDmmHardwareDriver
{
    public void Connect(string connectionTarget)
    {
        _ = connectionTarget;
    }

    public void Disconnect()
    {
    }

    public string Identify(string address, int channel)
    {
        return $"DMM-DEMO@{address}:CH{channel}";
    }

    public decimal MeasureVoltage(string address, int channel, decimal range)
    {
        var seed = (address.GetHashCode() & 0x7fffffff) % 100;
        return 3.0m + channel * 0.1m + (range > 10 ? 0.05m : 0m) + seed / 1000m;
    }
}
