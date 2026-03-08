namespace Ate.Engine.Hardware;

public interface IPsuHardwareDriver
{
    void Connect(string ip);

    void Disconnect();

    string Identify(string ip, int channel);

    void SetVoltage(int channel, decimal voltage, decimal currentLimit);

    void SetCurrentLimit(int channel, decimal currentLimit);

    void SetOutput(int channel, bool enabled);
}
