using Ate.Engine.DemoDrivers;
using Ate.Engine.DeviceIntegration.Providers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class DmmDriverModule : IDriverModule
{
    public string Name => "DMM";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IDmmHardwareDriverFactory, DemoDmmHardwareDriverFactory>();
        services.AddSingleton<IConfiguredWrapperProvider, DmmConfiguredWrapperProvider>();
    }
}
