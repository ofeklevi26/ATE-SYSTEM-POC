using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class SimulatedVendorPsuHardwareDriverFactory : IPsuHardwareDriverFactory
{
    public IPsuHardwareDriver Create()
    {
        return new SimulatedVendorPsuHardwareDriver();
    }
}
