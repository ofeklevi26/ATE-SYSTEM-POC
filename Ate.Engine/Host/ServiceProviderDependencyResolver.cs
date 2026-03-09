using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine;

public sealed class ServiceProviderDependencyResolver : IDependencyResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDependencyScope BeginScope()
    {
        return this;
    }

    public object? GetService(Type serviceType)
    {
        return _serviceProvider.GetService(serviceType);
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        return _serviceProvider.GetServices(serviceType);
    }

    public void Dispose()
    {
    }
}
