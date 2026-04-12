using Ate.Engine.Drivers;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DeviceIntegration.Modules;

public sealed class NiDaqMxDriverModule : IDriverModule
{
    public string Name => "NiDaqMx";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(new ConfiguredWrapperDescriptor("NiDaqMx", typeof(NiDaqMxDeviceWrapper)));
    }
}
