using Ate.Engine.Configuration;

namespace Ate.Engine.Wrappers;

public static class ConnectionEndpointResolver
{
    public static string Resolve(DriverInstanceConfiguration configuration)
    {
        var address = DriverConfigurationValueResolver.ResolveAddress(configuration);
        var channel = DriverConfigurationValueResolver.ResolveChannel(configuration);

        if (DriverConfigurationValueResolver.TryGetSetting(configuration, "endpoint", out var endpoint))
        {
            return endpoint;
        }

        if (DriverConfigurationValueResolver.TryGetSetting(configuration, "endpointFormat", out var endpointFormat))
        {
            var port = DriverConfigurationValueResolver.ResolvePort(configuration);
            var tokens = DriverConfigurationValueResolver.BuildDefaultTokens(configuration, address, channel, port);
            return DriverConfigurationValueResolver.ApplyTemplate(endpointFormat, tokens);
        }

        return address;
    }
}
