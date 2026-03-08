namespace Ate.Engine.Hardware;

public interface IDmmHardwareDriver
{
    void Connect(string ip);

    void Disconnect();

    string Identify(string ip, int channel);

    decimal MeasureVoltage(string ip, int channel, decimal range);
}
