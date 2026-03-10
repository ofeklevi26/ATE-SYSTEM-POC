using Ate.Engine.DemoDrivers;
using Ate.Engine.DeviceIntegration.Providers;
using Ate.Engine.Drivers;
using Ate.Engine.Hardware;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class PsuDriverModule : IDriverModule
{
    public string Name => "PSU";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IPsuHardwareDriverFactory, DemoPsuHardwareDriverFactory>();
        services.AddSingleton<IConfiguredWrapperProvider, PsuConfiguredWrapperProvider>();
    }
}
