using System.Web.Http;
using System.Web.Http.Dependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Ate.Engine;

public sealed class Startup
{
    private readonly IDependencyResolver _dependencyResolver;

    public Startup(IDependencyResolver dependencyResolver)
    {
        _dependencyResolver = dependencyResolver;
    }

    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        config.MapHttpAttributeRoutes();
        config.DependencyResolver = _dependencyResolver;

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

        app.UseWebApi(config);
    }
}
