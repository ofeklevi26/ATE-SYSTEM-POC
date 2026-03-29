namespace Ate.Engine.Hardware;

public interface INiDaqMxHardwareDriver
{
    void Connect(string connectionTarget);

    void Disconnect();

    string Identify(string address, int channel);

    string SetContiniousFrequency(string address, int channel, decimal frequency, decimal dutyCycle, bool isIdleStateHugh);
}
