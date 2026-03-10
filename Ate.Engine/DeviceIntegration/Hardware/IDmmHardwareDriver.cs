namespace Ate.Engine.Hardware;

public interface IDmmHardwareDriver
{
    void Connect(string connectionTarget);

    void Disconnect();

    string Identify(string address, int channel);

    decimal MeasureVoltage(string address, int channel, decimal range);
}
