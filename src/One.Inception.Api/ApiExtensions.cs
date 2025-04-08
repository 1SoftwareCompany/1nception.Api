using Microsoft.Extensions.DependencyInjection;

namespace One.Inception.Api;

public static class ApiExtensions
{
    public static IServiceCollection AddInceptionApi(this IServiceCollection services)
    {
        services.AddTransient<EventStoreExplorer>();
        services.AddTransient<ProjectionExplorer>();

        return services;
    }
}
