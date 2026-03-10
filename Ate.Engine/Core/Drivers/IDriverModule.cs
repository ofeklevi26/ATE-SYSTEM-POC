using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.Drivers;

public interface IDriverModule
{
    string Name { get; }

    void Register(IServiceCollection services);
}
