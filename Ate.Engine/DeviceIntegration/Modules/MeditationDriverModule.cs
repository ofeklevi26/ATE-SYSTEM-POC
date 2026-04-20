using Ate.Engine.Drivers;
using Ate.Engine.Wrappers;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.Modules;

public sealed class MeditationDriverModule : IDriverModule
{
    public string Name => "Meditation";

    public void Register(IServiceCollection services)
    {
        services.AddSingleton(new ConfiguredWrapperDescriptor("Meditation", typeof(MeditationDeviceWrapper)));
    }
}
