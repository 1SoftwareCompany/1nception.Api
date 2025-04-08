using Microsoft.Extensions.DependencyInjection;
using System;

namespace One.Inception.Api;

public static class MonitorClientServiceCollectionExtensioins
{
    public static IServiceCollection AddMonitor(this IServiceCollection services)
    {
        var newMonitorApiAddress = new Uri("http://localhost:5227"); // TODO: Speak with the team and figure out where should we put the configuration about monitor api
        services
            .AddHttpClient<MonitorClient>(client => client.BaseAddress = newMonitorApiAddress);

        return services;
    }
}
