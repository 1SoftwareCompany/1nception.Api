using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace One.Inception.Api.Hubs;

public class RebuildProjectionHub : Hub { }

public static class RebuildProjectionHubExtensions
{
    public static async Task ReportProgressAsync(this IHubContext<RebuildProjectionHub> hub, string projectionTypeId, ulong processedCount, ulong totalCount, CancellationToken cancellationToken = default)
    {
        if (hub?.Clients?.All is null == false)
        {
            await hub.Clients.All.SendCoreAsync("RebuildProgress", new object[] { projectionTypeId, processedCount, totalCount }, cancellationToken);
        }
    }

    public static async Task RebuildStartedAsync(this IHubContext<RebuildProjectionHub> hub, string projectionTypeId, CancellationToken cancellationToken = default)
    {
        if (hub?.Clients?.All is null == false)
        {
            await hub.Clients.All.SendCoreAsync("RebuildStarted", new object[] { projectionTypeId }, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task RebuildFinishedAsync(this IHubContext<RebuildProjectionHub> hub, string projectionTypeId, CancellationToken cancellationToken = default)
    {
        if (hub?.Clients?.All is null == false)
        {
            await hub.Clients.All.SendCoreAsync("RebuildFinished", new object[] { projectionTypeId }, cancellationToken).ConfigureAwait(false);
        }
    }
}
