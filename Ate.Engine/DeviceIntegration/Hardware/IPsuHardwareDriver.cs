using Ate.Engine.Infrastructure;

namespace Ate.Engine.Hardware;

public interface IPsuDriverBuilder
{
    void SetEndpoint(string endpoint, ILogger? logger = null);

    IPsuDriverAdapter BuildPsuDriverAdapter();
}

public interface IPsuDriverAdapter
{
    void Connect();

    void Disconnect();

    string Identify(string address, int channel);

    void SetVoltage(int channel, decimal voltage, decimal currentLimit);

    void SetCurrentLimit(int channel, decimal currentLimit);

    void SetOutput(int channel, bool enabled);
}
