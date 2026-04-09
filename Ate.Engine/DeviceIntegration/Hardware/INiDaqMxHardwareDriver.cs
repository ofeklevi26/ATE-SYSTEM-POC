using Ate.Engine.Infrastructure;

namespace Ate.Engine.Hardware;

public interface INiDaqMxDriverBuilder
{
    INiDaqMxDriverAdapter BuildDaqMxDriverAdapter(string deviceName, ILogger? logger = null);
}

public interface INiDaqMxDriverAdapter
{
    void Connect();

    void Disconnect();

    string Identify(string address, int channel);

    string SetContiniousFrequency(string address, int channel, decimal frequency, decimal dutyCycle, bool isIdleStateHugh);
}
