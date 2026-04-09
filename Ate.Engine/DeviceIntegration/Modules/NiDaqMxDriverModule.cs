using Ate.Engine.DemoDrivers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class NiDaqMxDriverModule : IDriverModule
{
    public string Name => "NiDaqMx";

    public void Register(IServiceCollection services)
    {
        services.AddTransient<INiDaqMxDriverBuilder, NiDaqMxHardwareDriverBuilder>();
        services.AddSingleton(new ConfiguredWrapperDescriptor("NiDaqMx", typeof(NiDaqMxDeviceWrapper)));
    }
}
