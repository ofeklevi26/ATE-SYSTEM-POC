using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoPsuHardwareDriverFactory : IPsuHardwareDriverFactory
{
    public IPsuHardwareDriver Create()
    {
        return new DemoPsuHardwareDriver();
    }
}
