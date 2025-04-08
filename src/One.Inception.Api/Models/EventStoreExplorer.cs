using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One.Inception.EventStore;
using One.Inception.EventStore.Index;
using One.Inception.Projections.Cassandra.EventSourcing;
using Microsoft.Extensions.Options;

namespace One.Inception.Api;

public class EventStoreExplorer
{
    private readonly IEventStore eventStore;
    private readonly BoundedContext boundedContext;
    private readonly ISerializer _serializer;

    public EventStoreExplorer(IEventStore eventStore, IOptionsMonitor<BoundedContext> boundedContextMonitor, ISerializer serializer)
    {
        if (ReferenceEquals(null, eventStore) == true) throw new ArgumentNullException(nameof(eventStore));

        this.eventStore = eventStore;
        this.boundedContext = boundedContextMonitor.CurrentValue;
        _serializer = serializer;
    }

    public async Task<AggregateDto> ExploreAsync(AggregateRootId id)
    {
        EventStream stream = await eventStore.LoadAsync(id).ConfigureAwait(false);
        if (stream.Commits.Any() == false) return new AggregateDto();

        var commitsDto = stream.Commits.Select(commit => BuildAggregateCommitDto(commit)).ToList();

        var arDto = new AggregateDto()
        {
            BoundedContext = boundedContext.Name,
            AggregateId = id.Value,
            Commits = commitsDto
        };
        return arDto;
    }

    public async Task<ExploreWithPagingResponse> ExploreEventsWithPagingAsync(AggregateRootId id, PagingOptions options)
    {
        LoadAggregateRawEventsWithPagingResult loadResult = await eventStore.LoadWithPagingAsync(id, options);
        List<RawEventDto> result = loadResult.RawEvents.Select(x => GetRawEventDto(x)).ToList();

        return new ExploreWithPagingResponse(result, loadResult.Options.PaginationToken);
    }

    public async Task<RepublishEventData> FindEventAsync(AggregateRootId id, int commitRevision, int eventPosition)
    {
        EventStream stream = await eventStore.LoadAsync(id).ConfigureAwait(false);

        AggregateCommit commit = stream.Commits.Where(commit => commit.Revision == commitRevision).SingleOrDefault();
        if (commit is null == false)
        {
            if (commit.Events.Count > eventPosition)
            {
                return new RepublishEventData(commit.Events[eventPosition].Unwrap(), commit.Timestamp);
            }
        }

        return null;
    }

    public RawEventDto GetRawEventDto(AggregateEventRaw @event)
    {
        IMessage messageData = _serializer.DeserializeFromBytes<IMessage>(@event.Data);
        return messageData.ToRawEventDto(DateTimeOffset.FromFileTime(@event.Timestamp), @event.Position, @event.Revision);
    }

    public async Task<IPublicEvent> FindPublicEventAsync(AggregateRootId id, int commitRevision, int eventPosition)
    {
        EventStream stream = await eventStore.LoadAsync(id).ConfigureAwait(false);

        AggregateCommit commit = stream.Commits.Where(commit => commit.Revision == commitRevision).SingleOrDefault();
        if (commit is null == false)
        {
            if (commit.Events.Count > eventPosition)
            {
                return commit.PublicEvents[eventPosition];
            }
        }

        return null;
    }

    public async Task<AggregateEventRaw> GetAggregateEventRaw(IndexRecord record)
    {
        AggregateEventRaw @event = await eventStore.LoadAggregateEventRaw(record).ConfigureAwait(false);
        return @event;
    }

    private static AggregateCommitDto BuildAggregateCommitDto(AggregateCommit commit)
    {
        IEnumerable<EventDto> events = commit.Events.ToEventDto(DateTimeOffset.FromFileTime(commit.Timestamp));
        int lastEventPosition = events.Max(e => e.EventPosition);
        IEnumerable<EventDto> publicEvents = commit.PublicEvents.ToPublicEventDto(lastEventPosition);

        return new AggregateCommitDto()
        {
            AggregateRootRevision = commit.Revision,
            Events = events.Union(publicEvents).ToList(),
            Timestamp = DateTime.FromFileTimeUtc(commit.Timestamp)
        };
    }

    public class AggregateDto
    {
        public string BoundedContext { get; set; }

        public string AggregateId { get; set; }

        public List<AggregateCommitDto> Commits { get; set; }
    }

    public class AggregateCommitDto
    {
        public int AggregateRootRevision { get; set; }

        public List<EventDto> Events { get; set; }

        public DateTime Timestamp { get; set; }
    }
}

public class EventDto
{
    public string Id { get; set; }

    public string EventName { get; set; }

    public object EventData { get; set; }

    public bool IsEntityEvent { get; set; }

    public bool IsPublicEvent { get; set; }

    public string EntityId { get; set; }

    public int EventPosition { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}

public class RawEventDto
{
    public string Id { get; set; }

    public string EventName { get; set; }

    public object EventData { get; set; }

    public bool IsEntityEvent { get; set; }

    public bool IsPublicEvent { get; set; }

    public string EntityId { get; set; }

    public int EventPosition { get; set; }

    public int EventRevision { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}

public class ExploreWithPagingResponse
{
    public ExploreWithPagingResponse(List<RawEventDto> events, byte[] paginationToken)
    {
        Events = events;
        PaginationToken = paginationToken;
    }

    public List<RawEventDto> Events { get; set; }

    public byte[] PaginationToken { get; set; }

    public static ExploreWithPagingResponse Empty() => new ExploreWithPagingResponse(new List<RawEventDto>(), null);
}

public class RepublishEventData
{
    public RepublishEventData(IEvent eventToRepublish, long timestamp)
    {
        EventToRepublish = eventToRepublish;
        Timestamp = timestamp;
    }

    public IEvent EventToRepublish { get; private set; }

    public long Timestamp { get; private set; }
}

public class RepublishEventNew
{
    RepublishEventNew(IEvent @event)
    {
        IsPublicEvent = false;
        Event = @event;
    }

    RepublishEventNew(IPublicEvent @event)
    {
        IsPublicEvent = true;
        PublicEvent = @event;
    }

    public bool IsPublicEvent { get; private set; }

    public IEvent Event { get; private set; }

    public IPublicEvent PublicEvent { get; private set; }

    public static RepublishEventNew GetNormalEvent(IEvent @event) => new RepublishEventNew(@event);
    public static RepublishEventNew GetPublicEvent(IPublicEvent publicEvent) => new RepublishEventNew(publicEvent);
}

public class RepublishEventDataNew
{
    internal RepublishEventDataNew(long timestamp, IEvent @event)
    {
        Timestamp = timestamp;
        Event = @event;
    }

    public long Timestamp { get; private set; }

    public IEvent Event { get; private set; }
}
