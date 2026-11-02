using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using One.Inception.Discoveries;
using One.Inception.Projections;
using System.Threading;
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

    public async Task HandleAsync(RebuildProjectionProgress signal, CancellationToken cancellationToken)
    {
        await hub.ReportProgressAsync(signal.ProjectionTypeId, signal.ProcessedCount, signal.TotalCount, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(RebuildProjectionStarted signal, CancellationToken cancellationToken)
    {
        await hub.RebuildStartedAsync(signal.ProjectionTypeId, cancellationToken).ConfigureAwait(false);
    }

    public async Task HandleAsync(RebuildProjectionFinished signal, CancellationToken cancellationToken)
    {
        await hub.RebuildFinishedAsync(signal.ProjectionTypeId, cancellationToken).ConfigureAwait(false);
    }
}
