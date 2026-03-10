using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoPsuHardwareDriver : IPsuHardwareDriver
{
    private decimal _currentVoltage;
    private decimal _currentLimit;
    private bool _outputEnabled;

    public void Connect(string connectionTarget)
    {
        _ = connectionTarget;
    }

    public void Disconnect()
    {
    }

    public string Identify(string address, int channel)
    {
        return $"PSU-DEMO@{address}:CH{channel}";
    }

    public void SetVoltage(int channel, decimal voltage, decimal currentLimit)
    {
        _ = channel;
        _currentVoltage = voltage;
        _currentLimit = currentLimit;
    }

    public void SetCurrentLimit(int channel, decimal currentLimit)
    {
        _ = channel;
        _currentLimit = currentLimit;
    }

    public void SetOutput(int channel, bool enabled)
    {
        _ = channel;
        _outputEnabled = enabled;
    }
}
