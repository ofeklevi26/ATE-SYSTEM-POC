using System.Web.Http;
using Ate.Engine.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace Ate.Engine;

public sealed class Startup
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;

    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        config.MapHttpAttributeRoutes();
        config.DependencyResolver = new ServiceProviderDependencyResolver(ServiceProvider);

        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

        app.UseWebApi(config);
    }
}
