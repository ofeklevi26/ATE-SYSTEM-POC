using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace Ate.Engine;

public sealed class ServiceProviderDependencyResolver : IDependencyResolver
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    public IDependencyScope BeginScope()
    {
        return new ServiceProviderDependencyScope(_scopeFactory.CreateScope());
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

    private sealed class ServiceProviderDependencyScope : IDependencyScope
    {
        private readonly IServiceScope _scope;

        public ServiceProviderDependencyScope(IServiceScope scope)
        {
            _scope = scope;
        }

        public object? GetService(Type serviceType)
        {
            return _scope.ServiceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _scope.ServiceProvider.GetServices(serviceType);
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
