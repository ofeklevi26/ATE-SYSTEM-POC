namespace Ate.Engine.Hardware;

public interface INiDaqMxDriverBuilder
{
    INiDaqMxDriverAdapter BuildDaqMxDriverAdapter();
}

public interface INiDaqMxDriverAdapter
{
    void Connect();

    void Disconnect();

    string Identify(string cardNumber, int channel);

    string SetContiniousFrequency(string cardNumber, int channel, decimal frequency, decimal dutyCycle, bool isIdleStateHugh);
}
