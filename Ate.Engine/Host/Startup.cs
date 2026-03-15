using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.ExceptionHandling;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;
using Ate.Engine.Infrastructure;

namespace Ate.Engine;

public sealed class Startup
{
    private readonly IDependencyResolver _dependencyResolver;
    private readonly ILogger _logger;

    public Startup(IDependencyResolver dependencyResolver, ILogger logger)
    {
        _dependencyResolver = dependencyResolver;
        _logger = logger;
    }

    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        config.MapHttpAttributeRoutes();
        config.DependencyResolver = _dependencyResolver;
        config.Services.Add(typeof(IExceptionLogger), new ApiExceptionLogger(_logger));

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

        app.UseWebApi(config);
    }
}
