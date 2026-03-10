using Ate.Engine.DemoDrivers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class DmmDriverModule : IDriverModule
{
    public string Name => "DMM";

    public void Register(IServiceCollection services)
    {
        services.AddTransient<IDmmHardwareDriver, DemoDmmHardwareDriver>();
        services.AddSingleton(new ConfiguredWrapperDescriptor("DMM", typeof(DmmDeviceWrapper)));
    }
}
