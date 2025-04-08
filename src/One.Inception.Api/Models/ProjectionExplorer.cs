using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using One.Inception.EventStore;
using One.Inception.MessageProcessing;
using One.Inception.Projections;
using One.Inception.Projections.Cassandra;
using One.Inception.Projections.Versioning;

namespace One.Inception.Api;

public class ProjectionExplorer
{
    private readonly IProjectionReader projections;
    private readonly IProjectionStore projectionStore;
    private readonly IInceptionContextAccessor contextAccessor;

    public ProjectionExplorer(IProjectionReader projections, IProjectionStore projectionStore, IInceptionContextAccessor contextAccessor)
    {
        if (ReferenceEquals(null, projections) == true) throw new ArgumentNullException(nameof(projections));
        if (ReferenceEquals(null, projectionStore) == true) throw new ArgumentNullException(nameof(projectionStore));
        if (ReferenceEquals(null, contextAccessor) == true) throw new ArgumentNullException(nameof(contextAccessor));

        this.projections = projections;
        this.projectionStore = projectionStore;
        this.contextAccessor = contextAccessor;
    }

    public async Task<ProjectionDto> ExploreAsync(IBlobId id, Type projectionType, DateTimeOffset? asOf = null)
    {
        var result = new ProjectionDto();
        ReadResult<IProjectionDefinition> projectionResult;
        if (asOf.HasValue)
        {
            projectionResult = await projections.GetAsOfAsync(id, projectionType, asOf.Value).ConfigureAwait(false);
        }
        else
        {
            projectionResult = await projections.GetAsync(id, projectionType).ConfigureAwait(false);
        }

        if (projectionResult.IsSuccess)
        {
            result.Name = projectionType.Name;
            result.State = projectionResult.Data.State;
        }
        return result;
    }

    public async Task<ProjectionDto> ExploreIncludingEventsAsync(IBlobId id, Type projectionType, DateTimeOffset? asOf)
    {
        ProjectionDto result = await ExploreAsync(id, projectionType, asOf).ConfigureAwait(false);

        bool projectionHasState = result.State is null == false;
        if (projectionHasState)
        {
            ProjectionVersion liveVersion = await GetLiveVersion(projectionType);

            ProjectionStream stream = ProjectionStream.Empty();
            if (liveVersion is null == false)
            {
                ProjectionQueryOptions options;
                if (asOf.HasValue)
                {
                    options = new ProjectionQueryOptions(id, liveVersion, asOf.Value);
                }
                else
                {
                    options = new ProjectionQueryOptions(id, liveVersion, new PagingOptions(1000, null, Order.Ascending));
                }

                ProjectionsOperator @operator = new ProjectionsOperator()
                {
                    OnProjectionStreamLoadedAsync = projectionStream =>
                    {
                        stream = projectionStream;
                        return Task.CompletedTask;
                    }
                };

                await projectionStore.EnumerateProjectionsAsync(@operator, options).ConfigureAwait(false);
            }

            foreach (IEvent @event in stream)
            {
                result.Commits.Add(@event.ToProjectionDto());
            }
        }

        return result;
    }

    private async Task<ProjectionVersion> GetLiveVersion(Type projectionType)
    {
        var projectionVersionManagerId = new ProjectionVersionManagerId(projectionType.GetContractId(), contextAccessor.Context.Tenant);
        ProjectionDto dto = await ExploreAsync(projectionVersionManagerId, typeof(ProjectionVersionsHandler)).ConfigureAwait(false);
        var state = dto?.State as ProjectionVersionsHandlerState;

        ProjectionVersion liveVersion = state is null ? null : state.AllVersions.GetLive();
        return liveVersion;
    }
}

public class ProjectionDto
{
    public ProjectionDto()
    {
        Commits = new List<ProjectionCommitDto>();
    }

    public string Name { get; set; }
    public object State { get; set; }
    public List<ProjectionCommitDto> Commits { get; set; }
}

public class ProjectionCommitDto
{
    public ProjectionCommitDto()
    {
        Events = new List<EventDto>();
    }

    public IEnumerable<EventDto> Events { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
