namespace Ate.Engine.Hardware;

public interface IPsuHardwareDriver
{
    void Connect(string connectionTarget);

    void Disconnect();

    string Identify(string address, int channel);

    void SetVoltage(int channel, decimal voltage, decimal currentLimit);

    void SetCurrentLimit(int channel, decimal currentLimit);

    void SetOutput(int channel, bool enabled);
}
