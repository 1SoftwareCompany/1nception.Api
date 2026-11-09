using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using One.Inception;
using One.Inception.Api.Controllers;
using One.Inception.DangerZone;
using One.Inception.Discoveries;
using One.Inception.EventStore;
using One.Inception.EventStore.Index;
using One.Inception.EventStore.Index.Handlers;
using One.Inception.EventStore.Players;
using One.Inception.MessageProcessing;
using One.Inception.Multitenancy;
using One.Inception.Projections;
using One.Inception.Projections.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static One.Inception.Api.EventStoreExplorer;

namespace One.Inception.Api;

/// <summary>
/// A facade with the whole management functionality of 1nception: exploring aggregates and projections,
/// replaying events, managing projections and indices, wiping data and so on.
/// <para>
/// It is designed to be registered in the service collection (<see cref="ApiExtensions.AddInceptionApi"/>) and
/// injected into any component — including the controllers of the built-in HTTP API and into custom
/// controllers of the services that consume the <c>1nception.Api</c> package.
/// </para>
/// <para>
/// Every public method mirrors a controller action of the original HTTP API with the same parameter
/// and return types, so the HTTP controllers became thin adapters over this class.
/// </para>
/// </summary>
public class InceptionManagement
{
    private readonly EventStoreExplorer eventExplorer;
    private readonly ProjectionExplorer projectionExplorer;
    private readonly IPublisher<IEvent> eventPublisher;
    private readonly IPublisher<IPublicEvent> publicEventPublisher;
    private readonly IPublisher<ICommand> commandPublisher;
    private readonly IPublisher<ISystemSignal> signalPublisher;
    private readonly IInceptionContextAccessor contextAccessor;
    private readonly IOptions<TenantsOptions> tenantsOptions;
    private readonly TypeContainer<IEventStoreIndex> eventStoreIndices;
    private readonly ProjectionHasher projectionHasher;
    private readonly DangerZoneExecutor dangerZoneExecutor;
    private readonly MonitorClient monitorClient;
    private readonly ILogger<InceptionManagement> logger;

    // The defaults are mutable on purpose: a call with explicit ReplayAfter/ReplayBefore
    // remembers the values for the next call of the same resolution (same behaviour as the original controllers).
    private DateTimeOffset replayAfterDefault = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));
    private DateTimeOffset replayBeforeDefault = new DateTimeOffset(2100, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));
    private DateTimeOffset replayPublicAfterDefault = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));
    private DateTimeOffset replayPublicBeforeDefault = new DateTimeOffset(2100, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));

    private static ICollection<Event_Response_New> cachedEvents;

    public InceptionManagement(
        EventStoreExplorer eventStoreExplorer,
        ProjectionExplorer projectionExplorer,
        IPublisher<IEvent> eventPublisher,
        IPublisher<IPublicEvent> publicEventPublisher,
        IPublisher<ICommand> commandPublisher,
        IPublisher<ISystemSignal> signalPublisher,
        IInceptionContextAccessor contextAccessor,
        IOptions<TenantsOptions> tenantsOptions,
        TypeContainer<IEventStoreIndex> eventStoreIndices,
        ProjectionHasher projectionHasher,
        DangerZoneExecutor dangerZoneExecutor,
        MonitorClient monitorClient,
        ILogger<InceptionManagement> logger)
    {
        if (eventStoreExplorer is null) throw new ArgumentNullException(nameof(eventStoreExplorer));
        if (projectionExplorer is null) throw new ArgumentNullException(nameof(projectionExplorer));
        if (eventPublisher is null) throw new ArgumentNullException(nameof(eventPublisher));
        if (publicEventPublisher is null) throw new ArgumentNullException(nameof(publicEventPublisher));
        if (commandPublisher is null) throw new ArgumentNullException(nameof(commandPublisher));
        if (signalPublisher is null) throw new ArgumentNullException(nameof(signalPublisher));
        if (contextAccessor is null) throw new ArgumentNullException(nameof(contextAccessor));
        if (tenantsOptions is null) throw new ArgumentNullException(nameof(tenantsOptions));
        if (eventStoreIndices is null) throw new ArgumentNullException(nameof(eventStoreIndices));
        if (projectionHasher is null) throw new ArgumentNullException(nameof(projectionHasher));
        if (dangerZoneExecutor is null) throw new ArgumentNullException(nameof(dangerZoneExecutor));
        if (monitorClient is null) throw new ArgumentNullException(nameof(monitorClient));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        this.eventExplorer = eventStoreExplorer;
        this.projectionExplorer = projectionExplorer;
        this.eventPublisher = eventPublisher;
        this.publicEventPublisher = publicEventPublisher;
        this.commandPublisher = commandPublisher;
        this.signalPublisher = signalPublisher;
        this.contextAccessor = contextAccessor;
        this.tenantsOptions = tenantsOptions;
        this.eventStoreIndices = eventStoreIndices;
        this.projectionHasher = projectionHasher;
        this.dangerZoneExecutor = dangerZoneExecutor;
        this.monitorClient = monitorClient;
        this.logger = logger;
    }

    /// <summary>
    /// Checks whether the service is alive. (was <c>PingController.Ping</c>)
    /// </summary>
    public IActionResult Ping()
    {
        return new OkObjectResult("pong");
    }

    /// <summary>
    /// Lists all commands known in the application. (was <c>CommandController.List</c>)
    /// </summary>
    public IActionResult ListCommands()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        var commandsMetaData = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(x => typeof(ICommand).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0));

        CommandListDto result = new CommandListDto();

        foreach (var cmd in commandsMetaData)
        {
            var metaToAdd = new CommandMeta();

            metaToAdd.Type = cmd.Name;
            metaToAdd.DataContract = cmd.GetContractId();

            result.Commands.Add(metaToAdd);
        }

        return new OkObjectResult(new ResponseResult<CommandListDto>(result));
    }

    /// <summary>
    /// Wipes all data of the given tenant. (was <c>DangerZoneController.WipeAsync</c>)
    /// </summary>
    public async Task<IActionResult> WipeAsync(string tenant)
    {
        await dangerZoneExecutor.WipeDataAsync(tenant).ConfigureAwait(false);

        return new OkObjectResult(new ResponseResult());
    }

    /// <summary>
    /// Explores the whole domain model: aggregates, events, commands, projections, sagas, ports and gateways. (was <c>DomainController.Explore</c>)
    /// </summary>
    public IActionResult ExploreDomain()
    {
        var result = new Domain_Response();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        result.Aggregates = GetAggregates(loadedAssemblies);
        result.Events = GetEvents(loadedAssemblies);
        result.Commands = GetCommands(loadedAssemblies);
        result.Projections = GetProjections(loadedAssemblies);
        result.Sagas = GetSagas(loadedAssemblies);
        result.Ports = GetPorts(loadedAssemblies);
        result.Gateways = GetGateways(loadedAssemblies);

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Gets a sample of an aggregate root id of the given tenant. (was <c>DomainController.GetAggregateRootIdSamples</c>)
    /// </summary>
    public IActionResult GetAggregateRootIdSamples(string tenant)
    {
        var result = new Domain_Response();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        result.AggregateIdSamples = GetAggregateIdSamples(loadedAssemblies, tenant);

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Gets the configured tenants. (was <c>DomainController.GetTenants</c>)
    /// </summary>
    public IActionResult GetTenants()
    {
        return new OkObjectResult(tenantsOptions.Value.Tenants);
    }

    /// <summary>
    /// Explores an aggregate of the event store. (was <c>EventStoreController.Explore</c>)
    /// </summary>
    public async Task<IActionResult> ExploreAggregate(EventStoreRequestModel model)
    {
        AggregateDto result = new AggregateDto();
        try
        {
            result = await eventExplorer.ExploreAsync(AggregateRootId.Parse(model.Id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to explore aggregate for {modelId}", model.Id);
        }

        return new OkObjectResult(new ResponseResult<AggregateDto>(result));
    }

    /// <summary>
    /// Explores an aggregate of the event store with paging. (was <c>EventStoreController.ExploreWithPaging</c>)
    /// </summary>
    public async Task<IActionResult> ExploreAggregateWithPaging(ExploreEventStoreWithPagingRequestModel model)
    {
        ExploreWithPagingResponse result = ExploreWithPagingResponse.Empty();
        PagingOptions options = new PagingOptions(model.Take, model.PaginationToken, Order.Descending);
        try
        {
            result = await eventExplorer.ExploreEventsWithPagingAsync(AggregateRootId.Parse(model.Id), options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to explore aggregate with paging. Id: {modelId}", model.Id);
        }

        return new OkObjectResult(new ResponseResult<ExploreWithPagingResponse>(result));
    }

    /// <summary>
    /// Republishes a single event of an aggregate to the given recipient handlers. (was <c>EventStoreController.Republish</c>)
    /// </summary>
    public async Task<IActionResult> Republish(RepublishRequest model)
    {
        var arId = AggregateRootId.Parse(model.Id);

        if (model.IsPublicEvent)
        {
            IPublicEvent @event = await eventExplorer.FindPublicEventAsync(arId, model.CommitRevision, model.EventPosition);

            if (@event is null) return new BadRequestObjectResult(new ResponseResult<string>("Event not found", "Event not found"));

            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { MessageHeader.AggregateRootId,  arId.Value}
            };

            await publicEventPublisher.PublishAsync(@event, headers);
        }
        else
        {
            RepublishEventData eventData = await eventExplorer.FindEventAsync(arId, model.CommitRevision, model.EventPosition);

            if (eventData is null) return new BadRequestObjectResult(new ResponseResult<string>("Event not found", "Event not found"));

            string recipientHandlers = ConcatRecipientHandlers(model.RecipientHandlers);

            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { MessageHeader.AggregateRootId,  arId.Value},
                { MessageHeader.AggregateRootRevision, model.CommitRevision.ToString()},
                { MessageHeader.AggregateRootEventPosition, model.EventPosition.ToString() },
                { MessageHeader.AggregateCommitTimestamp, eventData.Timestamp.ToString() },
                { MessageHeader.RecipientHandlers, string.Join(',', recipientHandlers) }
            };

            await eventPublisher.PublishAsync(eventData.EventToRepublish, headers);
        }

        return new OkObjectResult(new ResponseResult());
    }

    /// <summary>
    /// Republishes a single event of an aggregate based on its raw event record. (was <c>EventStoreController.RepublishNew</c>)
    /// </summary>
    public async Task<IActionResult> RepublishNew(RepublishRequestNew model)
    {
        AggregateRootId id = AggregateRootId.Parse(model.Id);
        IndexRecord record = new IndexRecord(model.EventContract, id.RawId, model.CommitRevision, model.EventPosition, model.Timestamp);
        AggregateEventRaw rawEvent = await eventExplorer.GetAggregateEventRaw(record).ConfigureAwait(false);

        byte[] rawData = rawEvent.Data;

        if (rawEvent is null)
            return new BadRequestObjectResult(new ResponseResult<string>("Event not found", "Event not found"));

        Type eventType = model.EventContract.GetTypeByContract();
        string tenant = contextAccessor.Context.Tenant;

        if (model.IsPublicEvent)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { MessageHeader.AggregateRootId,  id.Value},
                { MessageHeader.AggregateRootRevision, model.CommitRevision.ToString()},
                { MessageHeader.AggregateRootEventPosition, model.EventPosition.ToString() },
                { MessageHeader.AggregateCommitTimestamp, rawEvent.Timestamp.ToString() },
            };

            await publicEventPublisher.PublishAsync(rawData, eventType, tenant, headers);
        }
        else
        {
            string recipientHandlers = ConcatRecipientHandlers(model.RecipientHandlers);
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { MessageHeader.AggregateRootId,  id.Value},
                { MessageHeader.AggregateRootRevision, model.CommitRevision.ToString()},
                { MessageHeader.AggregateRootEventPosition, model.EventPosition.ToString() },
                { MessageHeader.AggregateCommitTimestamp, rawEvent.Timestamp.ToString() },
                { MessageHeader.RecipientHandlers, string.Join(',', recipientHandlers) }
            };

            await eventPublisher.PublishAsync(rawData, eventType, tenant, headers);
        }

        return new OkObjectResult(new ResponseResult());
    }

    /// <summary>
    /// Gets the metadata of the event store indices. (was <c>EventStoreIndexController.Meta</c>)
    /// </summary>
    public async Task<IActionResult> GetEventStoreIndexMeta()
    {
        List<MetaResponseModel> result = new List<MetaResponseModel>();

        foreach (var index in eventStoreIndices.Items)
        {
            var status = await projectionExplorer.ExploreAsync(new EventStoreIndexManagerId(index.GetContractId(), contextAccessor.Context.Tenant), typeof(EventStoreIndexStatus));

            MetaResponseModel indexResponse = new MetaResponseModel()
            {
                Id = index.GetContractId(),
                Name = index.Name,
                Status = status.State.ToString()
            };

            result.Add(indexResponse);
        }

        return new OkObjectResult(new ResponseResult<List<MetaResponseModel>>(result));
    }

    /// <summary>
    /// Rebuilds the whole event store index. (was <c>EventStoreIndexController.Rebuild</c>)
    /// </summary>
    public async Task<IActionResult> RebuildEventStoreIndex(RebuildIndexRequestModel model)
    {
        var command = new RebuildIndexCommand(new EventStoreIndexManagerId(model.Id, contextAccessor.Context.Tenant), model.MaxDegreeOfParallelism);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(RebuildIndexCommand)}'", $"Unable to publish command '{nameof(RebuildIndexCommand)}'"));
    }

    /// <summary>
    /// Replays a single event to a bounded context. (was <c>ReplayEventController.ReplayEvent</c>)
    /// </summary>
    public async Task<IActionResult> ReplayEvent(ReplayEventRequest model)
    {
        if (model.ReplayAfter.HasValue)
            replayAfterDefault = model.ReplayAfter.Value;

        if (model.ReplayBefore.HasValue)
            replayBeforeDefault = model.ReplayBefore.Value;

        ICollection<Event_Response_New> allEvents = GetAndCacheEvents();

        Event_Response_New found = allEvents.Where(x => x.Id.Equals(model.SourceEventTypeId)).SingleOrDefault();
        if (found is null)
            return new BadRequestObjectResult(new ResponseResult<string>($"Unable to start replay. This event is not found.", $"Unable to start replay. This event is not found."));

        Urn urn = null;
        if (string.IsNullOrEmpty(model.AggregateId) == false)
            urn = new Urn(model.AggregateId);

        if (found.IsPublicEvent)
        {
            if (string.IsNullOrEmpty(found.BC))
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'. BC of public event not found.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'. BC of public event not found."));

            if (model.RecipientBoundedContext.Equals(found.BC, StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish public event in the same bounded context.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish public event in the same bounded context."));

            var replay = new ReplayPublicEventsRequested()
            {
                Tenant = model.Tenant,
                RecipientBoundedContext = model.RecipientBoundedContext,
                RecipientHandlers = model.RecipientHandlers,
                SourceEventTypeId = model.SourceEventTypeId,
                ReplayOptions = new ReplayEventsOptionNew()
                {
                    After = replayAfterDefault,
                    Before = replayBeforeDefault,
                    AggregateRootId = urn,
                    ShouldReplayLastEventOnly = model.ShouldReplayLastEventOnly,
                },
            };

            if (await signalPublisher.PublishAsync(replay))
                return new OkObjectResult(new ResponseResult());
        }
        else
        {
            if (string.IsNullOrEmpty(found.BC) || model.RecipientBoundedContext.Equals(found.BC, StringComparison.OrdinalIgnoreCase))
            {
                var replay = new ReplayInternalEventsRequested()
                {
                    Tenant = model.Tenant,
                    RecipientBoundedContext = model.RecipientBoundedContext,
                    RecipientHandlers = model.RecipientHandlers,
                    SourceEventTypeId = model.SourceEventTypeId,
                    ReplayOptions = new ReplayEventsOptionNew()
                    {
                        After = replayAfterDefault,
                        Before = replayBeforeDefault,
                        AggregateRootId = urn,
                        ShouldReplayLastEventOnly = model.ShouldReplayLastEventOnly,
                    },
                };

                if (await signalPublisher.PublishAsync(replay))
                    return new OkObjectResult(new ResponseResult());
            }
            else
            {
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish internal event in another bounded context.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish internal event in another bounded context."));
            }
        }

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'"));
    }

    /// <summary>
    /// Replays a single public event to a bounded context. (was <c>ReplayPublicEventController.ReplayPublicEvent</c>)
    /// </summary>
    public async Task<IActionResult> ReplayPublicEvent(ReplayPublicEventRequest model)
    {
        if (model.ReplayAfter.HasValue)
            replayPublicAfterDefault = model.ReplayAfter.Value;

        if (model.ReplayBefore.HasValue)
            replayPublicBeforeDefault = model.ReplayBefore.Value;

        var replay = new ReplayPublicEventsRequested()
        {
            Tenant = model.Tenant,
            RecipientBoundedContext = model.RecipientBoundedContext,
            RecipientHandlers = model.RecipientHandlers,
            SourceEventTypeId = model.SourceEventTypeId,
            ReplayOptions = new ReplayEventsOptionNew()
            {
                After = replayPublicAfterDefault,
                Before = replayPublicBeforeDefault
            }
        };

        if (await signalPublisher.PublishAsync(replay))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'"));
    }

    /// <summary>
    /// Explores a projection. (was <c>ProjectionController.ExploreAsync</c>)
    /// </summary>
    public async Task<IActionResult> ExploreProjection(ProjectionRequestModel model)
    {
        var projectionType = model.ProjectionName.GetTypeByContract();
        IBlobId id = GetId(model.Id);
        ProjectionDto result = await projectionExplorer.ExploreAsync(id, projectionType, model.AsOf).ConfigureAwait(false);
        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
    }

    /// <summary>
    /// Explores a projection including its events. (was <c>ProjectionController.ExploreEvents</c>)
    /// </summary>
    public async Task<IActionResult> ExploreProjectionEvents(ProjectionRequestModel model)
    {
        var projectionType = model.ProjectionName.GetTypeByContract();
        IBlobId id = GetId(model.Id);
        ProjectionDto result = await projectionExplorer.ExploreIncludingEventsAsync(id, projectionType, model.AsOf).ConfigureAwait(false);
        result.State = null;

        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
    }

    /// <summary>
    /// Lists all projections with their versions. (was <c>ProjectionListController.List</c>)
    /// </summary>
    public async Task<IActionResult> ListProjections()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        var projectionMetaData = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(x => typeof(IProjection).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0));

        ProjectionListDto result = new ProjectionListDto();
        foreach (var meta in projectionMetaData)
        {
            var id = new ProjectionVersionManagerId(meta.GetContractId(), contextAccessor.Context.Tenant);
            var dto = await projectionExplorer.ExploreAsync(id, typeof(ProjectionVersionsHandler));
            ProjectionVersionsHandlerState state = dto?.State as ProjectionVersionsHandlerState;

            ProjectionAttribute contract = meta
                .GetCustomAttributes(true).Where(attr => attr is ProjectionAttribute)
                .SingleOrDefault() as ProjectionAttribute;

            var metaProjection = new ProjectionMeta()
            {
                ProjectionContractId = meta.GetContractId(),
                ProjectionName = meta.Name,
                IsReplayable = contract is not null,
                IsRebuildable = contract is not null && contract.Persistence == ProjectionEventsPersistenceSetting.Persistent, // why would you want a new version for not persisted projection, only fixing is allowed
                IsSearchable = typeof(IProjectionDefinition).IsAssignableFrom(meta),
                AfterTimestampFromAttribute = contract.Timestamp
            };
            if (ReferenceEquals(null, state))
            {
                metaProjection.Versions.Add(new ProjectionVersionDto()
                {
                    Status = ProjectionStatus.NotPresent,
                    Hash = projectionHasher.CalculateHash(meta),
                    Revision = 0
                });
            }
            else
            {
                foreach (var ver in state.AllVersions)
                {
                    metaProjection.Versions.Add(new ProjectionVersionDto()
                    {
                        Hash = ver.Hash,
                        Revision = ver.Revision,
                        Status = ver.Status
                    });
                }
            }
            result.Projections.Add(metaProjection);
        }

        return new OkObjectResult(new ResponseResult<ProjectionListDto>(result));
    }

    /// <summary>
    /// Gets the metadata of a projection. (was <c>ProjectionMetaController.Meta</c>)
    /// </summary>
    public async Task<IActionResult> GetProjectionMeta(ProjectionMetaRequestModel model)
    {
        IEnumerable<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);
        Type metadata = loadedAssemblies
            .SelectMany(assembly => assembly.GetLoadableTypes()
            .Where(x => typeof(IProjection).IsAssignableFrom(x))
            .Where(x => x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0))
            .Where(x => x.GetContractId() == model.ProjectionContractId)
            .FirstOrDefault();

        if (metadata is null) return new BadRequestObjectResult(new ResponseResult<string>($"Projection with contract '{model.ProjectionContractId}' not found", $"Projection with contract '{model.ProjectionContractId}' not found"));

        var id = new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant);
        ProjectionDto dto = await projectionExplorer.ExploreAsync(id, typeof(ProjectionVersionsHandler));
        var state = dto?.State as ProjectionVersionsHandlerState;

        ProjectionAttribute contract = metadata
            .GetCustomAttributes(true).Where(attr => attr is ProjectionAttribute)
            .SingleOrDefault() as ProjectionAttribute;

        var metaProjection = new ProjectionMeta()
        {
            ProjectionContractId = metadata.GetContractId(),
            ProjectionName = metadata.Name,
            IsReplayable = contract is not null,
            IsRebuildable = contract is not null && contract.Persistence == ProjectionEventsPersistenceSetting.Persistent, // why would you want a new version for not persisted projection, only fixing is allowed
            IsSearchable = typeof(IProjectionDefinition).IsAssignableFrom(metadata),
            AfterTimestampFromAttribute = contract.Timestamp
        };

        if (state is null)
        {
            metaProjection.Versions.Add(new ProjectionVersionDto()
            {
                Status = ProjectionStatus.NotPresent,
                Hash = projectionHasher.CalculateHash(typeof(ProjectionVersionsHandler)),
                Revision = 0
            });
        }
        else
        {
            foreach (var ver in state.AllVersions)
            {
                metaProjection.Versions.Add(new ProjectionVersionDto()
                {
                    Hash = ver.Hash,
                    Revision = ver.Revision,
                    Status = ver.Status
                });
            }
        }

        return new OkObjectResult(new ResponseResult<ProjectionMeta>(metaProjection));
    }

    /// <summary>
    /// Pauses a version of a projection. (was <c>ProjectionCancelController.Pause</c>)
    /// </summary>
    public async Task<IActionResult> PauseProjection(ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new PauseProjectionVersion(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(NewProjectionVersion)}'", $"Unable to publish command '{nameof(NewProjectionVersion)}'"));
    }

    /// <summary>
    /// Cancels a version of a projection. (was <c>ProjectionCancelController.Cancel</c>)
    /// </summary>
    public async Task<IActionResult> CancelProjection(ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new CancelProjectionVersionRequest(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version, model.Reason ?? "Canceled by user");

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(CancelProjectionVersionRequest)}'", $"Unable to publish command '{nameof(CancelProjectionVersionRequest)}'"));
    }

    /// <summary>
    /// Finalizes a version of a projection. (was <c>ProjectionCancelController.Finalize</c>)
    /// </summary>
    public async Task<IActionResult> FinalizeProjection(ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new FinalizeProjectionVersionRequest(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeProjectionVersionRequest)}'", $"Unable to publish command '{nameof(FinalizeProjectionVersionRequest)}'"));
    }

    /// <summary>
    /// Fixes (repairs the state of) a version of a projection. (was <c>ProjectionRebuildController.Fix</c>)
    /// </summary>
    public async Task<IActionResult> FixProjectionVersion(ProjectionRebuildRequestModel model)
    {
        model.PlayerOptions ??= new PlayerOptions();
        var replayEventsOptions = new ReplayEventsOptions()
        {
            After = model.PlayerOptions.After,
            Before = model.PlayerOptions.Before
        };

        // This if statement should go inside the ReplayEventsOptions somehow
        if (model.PlayerOptions.MaxDegreeOfParallelism.HasValue)
            replayEventsOptions.MaxDegreeOfParallelism = model.PlayerOptions.MaxDegreeOfParallelism.Value;

        var command = new FixProjectionVersion(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), model.Hash, replayEventsOptions);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FixProjectionVersion)}'", $"Unable to publish command '{nameof(FixProjectionVersion)}'"));
    }

    /// <summary>
    /// Creates a new version of a projection. (was <c>ProjectionRebuildController.New</c>)
    /// </summary>
    public async Task<IActionResult> NewProjectionVersion(ProjectionRebuildRequestModel model)
    {
        model.PlayerOptions ??= new PlayerOptions();
        var replayEventsOptions = new ReplayEventsOptions()
        {
            After = model.PlayerOptions.After,
            Before = model.PlayerOptions.Before
        };

        // This if statement should go inside the ReplayEventsOptions somehow
        if (model.PlayerOptions.MaxDegreeOfParallelism.HasValue)
            replayEventsOptions.MaxDegreeOfParallelism = model.PlayerOptions.MaxDegreeOfParallelism.Value;

        var command = new NewProjectionVersion(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), model.Hash, replayEventsOptions);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(NewProjectionVersion)}'", $"Unable to publish command '{nameof(NewProjectionVersion)}'"));
    }

    /// <summary>
    /// Lists all event store indices with their status. (was <c>IndexListController.List</c>)
    /// </summary>
    public async Task<IActionResult> ListIndices()
    {
        IndexListDto result = new IndexListDto();
        foreach (var meta in eventStoreIndices.Items)
        {
            var id = new EventStoreIndexManagerId(meta.GetContractId(), contextAccessor.Context.Tenant);
            var dto = await projectionExplorer.ExploreAsync(id, typeof(EventStoreIndexStatus));
            EventStoreIndexStatusState state = dto?.State as EventStoreIndexStatusState;
            var metaIndex = new IndexMeta()
            {
                IndexContractId = meta.GetContractId(),
                IndexName = meta.Name,
            };
            if (ReferenceEquals(null, state))
            {
                metaIndex.Status = IndexStatus.NotPresent;
            }
            else
            {
                metaIndex.Status = state.Status;
            }
            result.Indices.Add(metaIndex);
        }

        return new OkObjectResult(new ResponseResult<IndexListDto>(result));
    }

    /// <summary>
    /// Rebuilds a specific event store index. (was <c>IndexRebuildActionsController.Rebuild</c>)
    /// </summary>
    public async Task<IActionResult> RebuildIndex(IndexRequestModel model)
    {
        var command = new RebuildIndexCommand(new EventStoreIndexManagerId(model.IndexContractId, contextAccessor.Context.Tenant), model.MaxDegreeOfParallelism);

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'", $"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'"));
    }

    /// <summary>
    /// Finalizes a specific event store index. (was <c>IndexRebuildActionsController.Finalize</c>)
    /// </summary>
    public async Task<IActionResult> FinalizeIndex(IndexRequestModel model)
    {
        var command = new FinalizeEventStoreIndexRequest(new EventStoreIndexManagerId(model.IndexContractId, contextAccessor.Context.Tenant));

        if (await commandPublisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'", $"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'"));
    }

    /// <summary>
    /// Gets the bounded contexts that are currently alive (registered in the monitor). (was <c>RecipientBoundedContextController</c>)
    /// </summary>
    public async Task<IActionResult> GetRecipientBoundedContexts()
    {
        List<string> liveBoundedContext = await monitorClient.GetBoundedContextListAsync().ConfigureAwait(false);

        return new OkObjectResult(liveBoundedContext);
    }

    /// <summary>
    /// Gets the tenants that are currently alive (registered in the monitor). (was <c>RecipientTenantController</c>)
    /// </summary>
    public async Task<IActionResult> GetTenantContexts()
    {
        List<string> liveTenant = await monitorClient.GetTenantListAsync().ConfigureAwait(false);

        return new OkObjectResult(liveTenant);
    }

    #region helpers (moved from the controllers)

    private IBlobId GetId(string theId)
    {
        if (theId.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            theId = theId[2..];
            byte[] bytes = Convert.FromHexString(theId);

            IBlobId idForSearch = new BlobIdForSearch(bytes);
            return idForSearch;
        }
        else
        {
            return new Urn(theId);
        }
    }

    private string ConcatRecipientHandlers(string[] chosenRecipientHandlers)
    {
        string projectionIndexContract = typeof(ProjectionIndex).GetContractId();
        string handlerContracts = string.Join(',', chosenRecipientHandlers);

        return $"{projectionIndexContract},{handlerContracts}";
    }

    private static ICollection<Event_Response_New> GetAndCacheEvents()
    {
        if (cachedEvents is not null)
            return cachedEvents;

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        ICollection<Event_Response_New> allEvents = GetReplayableEvents(loadedAssemblies);

        cachedEvents = allEvents;
        return cachedEvents;
    }

    private static ICollection<Event_Response_New> GetReplayableEvents(IEnumerable<Assembly> loadedAssemblies)
    {
        var events = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response_New
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               BC = meta.GetCustomAttribute<DataContractAttribute>().Namespace,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = false
           });

        var publicEvents = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IPublicEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response_New
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               BC = meta.GetCustomAttribute<DataContractAttribute>().Namespace,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = true
           });
        var allEvents = events.Concat(publicEvents);
        return allEvents.ToList();
    }

    private static ICollection<TResult> RetrieveTypesFromAssemblies<TResult>(IEnumerable<Assembly> loadedAssemblies, Func<Type, bool> typeFilter, Func<Type, TResult> retrieveResult)
    {
        var result = new List<TResult>();

        var typesMeta = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(typeFilter));

        foreach (var typeMeta in typesMeta)
        {
            result.Add(retrieveResult(typeMeta));
        }

        return result;
    }

    private bool HandlerRetrieveRequirements<T>(Type handlerCandidate)
    {
        return handlerCandidate.IsAbstract == false && handlerCandidate.IsInterface == false && typeof(T).IsAssignableFrom(handlerCandidate);
    }

    static Type AggregateRootIdType = typeof(AggregateRootId);
    private bool IsAggregateRootIdGeneric(Type candidate)
    {
        if (candidate.BaseType is null || candidate.BaseType.IsGenericType == false)
            return false;

        return candidate.IsAbstract == false && candidate.IsInterface == false && AggregateRootIdType.IsAssignableFrom(candidate.BaseType.GetGenericTypeDefinition());
    }

    private ICollection<Gateway_Response> GetGateways(IEnumerable<Assembly> loadedAssemblies)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
            x => HandlerRetrieveRequirements<IGateway>(x),
            meta => new Gateway_Response
            {
                Name = meta.Name,
                Events = meta
                            .GetInterfaces()
                                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandle<>))
                                .SelectMany(ff => ff.GetGenericArguments()).Where(x => typeof(IEvent).IsAssignableFrom(x))
                                .Select(x =>
                                    new Event_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    }).ToList()
            });
    }

    private ICollection<Port_Response> GetPorts(IEnumerable<Assembly> loadedAssemblies)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
            x => HandlerRetrieveRequirements<IPort>(x),
            meta => new Port_Response
            {
                Id = meta.GetContractId(),
                Name = meta.Name,
                Events = meta
                            .GetInterfaces()
                                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandle<>))
                                .SelectMany(ff => ff.GetGenericArguments()).Where(x => typeof(IEvent).IsAssignableFrom(x))
                                .Select(x =>
                                    new Event_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    }).ToList()
            });
    }

    private ICollection<Saga_Response> GetSagas(IEnumerable<Assembly> loadedAssemblies)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
           x => HandlerRetrieveRequirements<IProcessManager>(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Saga_Response
           {
               Id = meta.GetContractId(),
               Name = meta.Name,
               Events = meta
                            .GetInterfaces()
                                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandle<>))
                                .SelectMany(ff => ff.GetGenericArguments()).Where(x => typeof(IEvent).IsAssignableFrom(x))
                                .Select(x =>
                                    new Event_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    }).ToList()
           });
    }

    private ICollection<Projection_Response> GetProjections(IEnumerable<Assembly> loadedAssemblies)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
           x => HandlerRetrieveRequirements<IProjection>(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Projection_Response
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               IsEventSourced = IsEventSourced(meta),
               Events = meta
                            .GetInterfaces()
                                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEventHandle<>))
                                .SelectMany(ff => ff.GetGenericArguments()).Where(x => typeof(IEvent).IsAssignableFrom(x))
                                .Select(x =>
                                    new Event_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    }).ToList()
           });
    }

    private bool IsEventSourced(Type projection)
    {
        bool isEventSourced = typeof(IProjection).IsAssignableFrom(projection) && projection.IsPersistedProjection();
        return isEventSourced;
    }

    private ICollection<Command_Response> GetCommands(IEnumerable<Assembly> loadedAssemblies)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(ICommand).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Command_Response
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name
           });
    }

    private ICollection<Event_Response> GetEvents(IEnumerable<Assembly> loadedAssemblies)
    {
        var events = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = false
           });

        var publicEvents = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IPublicEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = true
           });
        var allEvents = events.Concat(publicEvents);
        return allEvents.ToList();
    }

    private ICollection<Aggregate_Response> GetAggregates(IEnumerable<Assembly> loadedAssemblies)
    {
        DefaulAssemblyScanner assemblyScanner = new DefaulAssemblyScanner();
        IEnumerable<Type> allEventsInAssembly = assemblyScanner.Scan().Where(x => typeof(IEvent).IsAssignableFrom(x));

        return RetrieveTypesFromAssemblies(loadedAssemblies,
            x => HandlerRetrieveRequirements<IAggregateRoot>(x) && x != typeof(AggregateRoot<>),
            meta => new Aggregate_Response
            {
                Name = meta.Name,
                Commands = GetAggregateAppService(loadedAssemblies, meta)
                                ?.GetInterfaces()
                                ?.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandHandle<>))
                                ?.SelectMany(ff => ff.GetGenericArguments()).Where(x => typeof(ICommand).IsAssignableFrom(x))
                                ?.Select(x =>
                                    new Command_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    })
                                ?.ToList(),
                Events = GetWhenEvents(GetAggregateState(meta), allEventsInAssembly)
            });
    }

    private ICollection<AggregateIdSample_Response> GetAggregateIdSamples(IEnumerable<Assembly> loadedAssemblies, string tenant)
    {
        return RetrieveTypesFromAssemblies(loadedAssemblies,
            x => IsAggregateRootIdGeneric(x),
            meta => new AggregateIdSample_Response
            {
                IdSample = ExtractSample(meta, tenant),
            });
    }

    const string AggregateRootId_MethodName_New = "New";
    delegate AggregateRootId New(ReadOnlySpan<char> tenant, ReadOnlySpan<char> id);
    private string ExtractSample(Type aggregateIdType, string tenant)
    {
        MethodInfo[] typeStaticMethods = aggregateIdType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        MethodInfo methodFactoryInfo = typeStaticMethods.Where(found => found.GetParameters().Length == 2 && found.Name == AggregateRootId_MethodName_New).Single();
        AggregateRootId idInstance = methodFactoryInfo.CreateDelegate<New>(null)(tenant.AsSpan(), "id");

        return idInstance.Value;
    }

    private ICollection<Event_Response> GetWhenEvents(Type type, IEnumerable<Type> allEventsInAssembly)
    {
        IEnumerable<Type> allEventTypesFromWhenMethods = type.GetMethods()
         .Where(x => x.GetParameters().Count() == 1 && typeof(IEvent).IsAssignableFrom(x.GetParameters().FirstOrDefault().ParameterType))
         .Select(x => x.GetParameters().FirstOrDefault().ParameterType);

        IEnumerable<Type> allNonAbstractEventsInWhenMethods = GetAllNonAbstractTypesViaDepthSearch(allEventTypesFromWhenMethods, allEventsInAssembly);

        return allNonAbstractEventsInWhenMethods.Select(x =>
                                    new Event_Response
                                    {
                                        Id = x.GetCustomAttribute<DataContractAttribute>().Name,
                                        Name = x.Name
                                    }).ToList();
    }

    private Type GetAggregateState(Type meta)
    {
        return meta.BaseType.GetGenericArguments().FirstOrDefault(x => typeof(IAggregateRootState).IsAssignableFrom(x));
    }

    private Type GetAggregateAppService(IEnumerable<Assembly> loadedAssemblies, Type aggregate)
    {
        var d1 = typeof(ApplicationService<>);
        Type[] typeArgs = { aggregate };
        var makeme = d1.MakeGenericType(typeArgs);

        var allTypes = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes());

        var appService = allTypes
            .Where(x => makeme.IsAssignableFrom(x) && x.IsInterface == false && x.IsAbstract == false)
            .SingleOrDefault(); // Just imagine that there is an Aggregate without an AppService, see?

        return appService;
    }

    /// <summary>
    /// That solves the situation when we have interface : interface : abstract class : abstract class : abstract class : class ... via Depth Search
    /// </summary>
    /// <param name="eventTypes">all event types in aggregate states</param>
    /// <param name="allEventTypesInAssembly">all event types in the assemblu</param>
    private IEnumerable<Type> GetAllNonAbstractTypesViaDepthSearch(IEnumerable<Type> eventTypes, IEnumerable<Type> allEventTypesInAssembly)
    {
        HashSet<Type> allClassesThatAreNotAbstract = new HashSet<Type>();

        foreach (var type in eventTypes)
        {
            var typeQueue = new Queue<Type>();
            typeQueue.Enqueue(type);
            while (typeQueue.Count != 0)
            {
                var item = typeQueue.Dequeue();

                if (item.IsAbstract == false)
                {
                    allClassesThatAreNotAbstract.Add(item);
                    continue;
                }

                var assignableFromEventType = allEventTypesInAssembly.Where(t => item.IsAssignableFrom(t) && t != item);
                foreach (var child in assignableFromEventType.Where(x => x.IsAbstract))
                    typeQueue.Enqueue(child);

                IEnumerable<Type> notAbstract = assignableFromEventType.Where(x => x.IsAbstract == false);
                allClassesThatAreNotAbstract.UnionWith(notAbstract);
            }
        }

        return allClassesThatAreNotAbstract;
    }

    #endregion
}
