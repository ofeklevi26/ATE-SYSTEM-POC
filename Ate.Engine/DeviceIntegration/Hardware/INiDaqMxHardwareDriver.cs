namespace Ate.Engine.Hardware;

public interface INiDaqMxDriverBuilder
{
    INiDaqMxDriverAdapter BuildDaqMxDriverAdapter();
}

public interface INiDaqMxDriverAdapter
{
    void Connect();

    void Disconnect();

    string Identify(int channel);

    string SetContiniousFrequency(int channel, decimal frequency, decimal dutyCycle, bool isIdleStateHugh);
}
