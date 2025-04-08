using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using One.Inception.Discoveries;
using One.Inception.Projections;
using System.Threading.Tasks;

namespace One.Inception.Api.Hubs;

public class DashboardUpdater : ISystemTrigger,
    ISignalHandle<RebuildProjectionProgress>,
    ISignalHandle<RebuildProjectionStarted>,
    ISignalHandle<RebuildProjectionFinished>
{
    private readonly IHubContext<RebuildProjectionHub> hub;

    public DashboardUpdater(IApiAccessor apiAccessor)
    {
        if (apiAccessor?.Provider is null == false)
            this.hub = apiAccessor.Provider.GetRequiredService<IHubContext<RebuildProjectionHub>>();
    }

    public async Task HandleAsync(RebuildProjectionProgress signal)
    {
        await hub.ReportProgressAsync(signal.ProjectionTypeId, signal.ProcessedCount, signal.TotalCount).ConfigureAwait(false);
    }

    public async Task HandleAsync(RebuildProjectionStarted signal)
    {
        await hub.RebuildStartedAsync(signal.ProjectionTypeId).ConfigureAwait(false);
    }

    public async Task HandleAsync(RebuildProjectionFinished signal)
    {
        await hub.RebuildFinishedAsync(signal.ProjectionTypeId).ConfigureAwait(false);
    }
}
