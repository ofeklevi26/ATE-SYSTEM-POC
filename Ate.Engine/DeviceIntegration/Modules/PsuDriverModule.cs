using Ate.Engine.Drivers;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class PsuDriverModule : IDriverModule
{
    public string Name => "PSU";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(new ConfiguredWrapperDescriptor("PSU", typeof(PsuDeviceWrapper)));
    }
}
