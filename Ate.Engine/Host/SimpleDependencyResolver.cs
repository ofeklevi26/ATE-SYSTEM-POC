using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http.Dependencies;

namespace Ate.Engine;

public sealed class SimpleDependencyResolver : IDependencyResolver
{
    private readonly IReadOnlyDictionary<Type, Func<object>> _registrations;

    public SimpleDependencyResolver(IDictionary<Type, Func<object>> registrations)
    {
        _registrations = new Dictionary<Type, Func<object>>(registrations);
    }

    public IDependencyScope BeginScope()
    {
        return this;
    }

    public object? GetService(Type serviceType)
    {
        if (_registrations.TryGetValue(serviceType, out var factory))
        {
            return factory();
        }

        if (serviceType.IsAbstract || serviceType.IsInterface)
        {
            return null;
        }

        var constructor = SelectConstructor(serviceType);
        if (constructor == null)
        {
            return null;
        }

        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var dependency = GetService(parameters[i].ParameterType);
            if (dependency == null)
            {
                return null;
            }

            arguments[i] = dependency;
        }

        return constructor.Invoke(arguments);
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        var singleService = GetService(serviceType);
        return singleService == null ? Enumerable.Empty<object>() : new[] { singleService };
    }

    public void Dispose()
    {
    }

    private static ConstructorInfo? SelectConstructor(Type serviceType)
    {
        return serviceType
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
    }
}
