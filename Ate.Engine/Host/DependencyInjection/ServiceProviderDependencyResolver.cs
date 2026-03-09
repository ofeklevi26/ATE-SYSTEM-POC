using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine.DependencyInjection;

public sealed class ServiceProviderDependencyResolver : IDependencyResolver
{
    private readonly IServiceScope? _scope;

    public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    private ServiceProviderDependencyResolver(IServiceScope scope)
    {
        _scope = scope;
        ServiceProvider = scope.ServiceProvider;
    }

    private IServiceProvider ServiceProvider { get; }

    public object? GetService(Type serviceType)
    {
        return ServiceProvider.GetService(serviceType);
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        return ServiceProvider.GetServices(serviceType);
    }

    public IDependencyScope BeginScope()
    {
        return new ServiceProviderDependencyResolver(ServiceProvider.CreateScope());
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}
