using Ate.Engine.Hardware;

namespace Ate.Engine.DemoDrivers;

public sealed class DemoDmmHardwareDriverFactory : IDmmHardwareDriverFactory
{
    public IDmmHardwareDriver Create()
    {
        return new DemoDmmHardwareDriver();
    }
}
