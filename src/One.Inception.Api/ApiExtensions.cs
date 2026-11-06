using Microsoft.Extensions.DependencyInjection;

namespace One.Inception.Api;

public static class ApiExtensions
{
    public static IServiceCollection AddInceptionApi(this IServiceCollection services)
    {
        services.AddTransient<EventStoreExplorer>();
        services.AddTransient<ProjectionExplorer>();

        // The facade with the whole management functionality. Transient on purpose:
        // some of the replay defaults are per-resolution state (same as the transient controllers had).
        services.AddTransient<InceptionManagement>();

        return services;
    }
}
