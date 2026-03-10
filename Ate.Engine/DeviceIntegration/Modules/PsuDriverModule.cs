using Ate.Engine.DemoDrivers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class PsuDriverModule : IDriverModule
{
    public string Name => "PSU";

    public void Register(IServiceCollection services)
    {
        services.AddTransient<IPsuHardwareDriver, DemoPsuHardwareDriver>();
        services.AddSingleton(new ConfiguredWrapperDescriptor("PSU", typeof(PsuDeviceWrapper)));
    }
}
