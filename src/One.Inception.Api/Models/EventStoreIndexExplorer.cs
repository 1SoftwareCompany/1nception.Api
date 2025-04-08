using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using One.Inception.EventStore;
using Microsoft.Extensions.Options;

namespace One.Inception.Api;

public class EventStoreIndexExplorer
{
    private readonly IEventStore eventStore;
    private readonly BoundedContext boundedContext;

    public EventStoreIndexExplorer(IEventStore eventStore, IOptionsMonitor<BoundedContext> boundedContextMonitor)
    {
        if (ReferenceEquals(null, eventStore) == true) throw new ArgumentNullException(nameof(eventStore));

        this.eventStore = eventStore;
        this.boundedContext = boundedContextMonitor.CurrentValue;
    }

    public async Task<AggregateDto> ExploreAsync(AggregateRootId id)
    {
        EventStream stream = await eventStore.LoadAsync(id).ConfigureAwait(false);
        if (stream.Commits.Count() == 0) return new AggregateDto();
        var commitsDto = stream.Commits.Select(commit =>
            new AggregateCommitDto()
            {
                AggregateRootRevision = commit.Revision,
                Events = commit.Events.Select(x => new EventDto() { EventName = x.GetType().Name, EventData = x }).ToList(),
                Timestamp = DateTime.FromFileTimeUtc(commit.Timestamp)
            }).ToList();

        var arDto = new AggregateDto()
        {
            BoundedContext = boundedContext.Name,
            AggregateId = id.Value,
            Commits = commitsDto
        };
        return arDto;
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

    public class EventDto
    {
        public string EventName { get; set; }

        public IEvent EventData { get; set; }
    }
}
