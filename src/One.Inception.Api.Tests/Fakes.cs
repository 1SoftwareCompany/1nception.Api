using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using One.Inception;
using One.Inception.DangerZone;
using One.Inception.EventStore;
using One.Inception.EventStore.Index;
using One.Inception.EventStore.Index.Handlers;
using One.Inception.MessageProcessing;
using One.Inception.Multitenancy;
using One.Inception.Projections;
using One.Inception.Projections.Cassandra;
using One.Inception.Projections.Versioning;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace One.Inception.Api.Tests;

#region test model (types discoverable by the facade's assembly scans)

[DataContract(Name = "bgib29-test-event", Namespace = "One.Inception.Api.Tests")]
public class Bgib29TestEvent : IEvent
{
    public Bgib29TestEvent()
    {
    }

    public string Payload { get; set; }
    public DateTimeOffset Timestamp => default;
    public string MessageId => Guid.NewGuid().ToString();
}

[DataContract(Name = "bgib29-test-public-event", Namespace = "One.Inception.Api.Tests")]
public class Bgib29TestPublicEvent : IPublicEvent
{
    public Bgib29TestPublicEvent()
    {
    }

    public string Payload { get; set; }
    public string Tenant => "tenant1";
    public DateTimeOffset Timestamp => default;
    public string MessageId => Guid.NewGuid().ToString();
}

[DataContract(Name = "bgib29-test-command")]
public class Bgib29TestCommand : ICommand
{
    public Bgib29TestCommand()
    {
    }

    public string Payload { get; set; }
    public DateTimeOffset Timestamp => default;
    public string MessageId => Guid.NewGuid().ToString();
}

public class Bgib29TestState : IAggregateRootState
{
    public AggregateRootId Id { get; set; }

    public void HandleTestEvent(Bgib29TestEvent @event)
    {
    }
}

[DataContract(Name = "bgib29-test-index")]
public class Bgib29TestIndex : IEventStoreIndex
{
    public Task IndexAsync(InceptionMessage message) => Task.CompletedTask;
}

public class Bgib29TestRoot : AggregateRoot<Bgib29TestState>
{
}

public abstract class Bgib29IdBase<TId> : AggregateRootId where TId : Bgib29IdBase<TId>
{
    protected Bgib29IdBase(string tenant, string arName, string id)
        : base(tenant, arName, id)
    {
    }
}

[DataContract(Name = "bgib29-test-id")]
public class Bgib29TestId : Bgib29IdBase<Bgib29TestId>
{
    public Bgib29TestId(string tenant, string id)
        : base(tenant, "Bgib29TestId", id)
    {
    }

    public static Bgib29TestId New(ReadOnlySpan<char> tenant, ReadOnlySpan<char> id)
    {
        return new Bgib29TestId(tenant.ToString(), id.ToString());
    }
}

#endregion

#region fakes

public class RecordingPublisher<TMessage> : IPublisher<TMessage> where TMessage : IMessage
{
    public bool Success { get; set; } = true;

    public List<(TMessage Message, Dictionary<string, string> Headers)> Published { get; } = new();

    public Task<bool> PublishAsync(TMessage message, Dictionary<string, string> messageHeaders)
    {
        Published.Add((message, messageHeaders));
        return Task.FromResult(Success);
    }

    public Task<bool> PublishAsync(TMessage message, DateTimeOffset publishAt, Dictionary<string, string> messageHeaders)
        => PublishAsync(message, messageHeaders);

    public Task<bool> PublishAsync(TMessage message, TimeSpan publishAfter, Dictionary<string, string> messageHeaders)
        => PublishAsync(message, messageHeaders);

    public Task<bool> PublishAsync(byte[] messageRaw, Type messageType, string tenant, Dictionary<string, string> messageHeaders)
    {
        throw new NotSupportedException();
    }
}

public class FakeEventStore : IEventStore
{
    public Task AppendAsync(AggregateCommit aggregateCommit) => Task.CompletedTask;

    public Task AppendAsync(AggregateEventRaw eventRaw) => Task.CompletedTask;

    public Task<EventStream> LoadAsync(IBlobId aggregateId) => Task.FromResult<EventStream>(null);

    public Task<bool> DeleteAsync(AggregateEventRaw eventRaw) => Task.FromResult(false);

    public Task<LoadAggregateRawEventsWithPagingResult> LoadWithPagingAsync(IBlobId aggregateId, PagingOptions pagingOptions)
        => Task.FromResult<LoadAggregateRawEventsWithPagingResult>(null);

    public Task<AggregateEventRaw> LoadAggregateEventRaw(IndexRecord indexRecord) => Task.FromResult<AggregateEventRaw>(null);
}

public class FakeProjectionReader : IProjectionReader
{
    public ReadResult<IProjectionDefinition> Result;

    public Task<ReadResult<T>> GetAsync<T>(IBlobId projectionId) where T : IProjectionDefinition
        => Task.FromResult(new ReadResult<T>(default));

    public Task<ReadResult<T>> GetAsOfAsync<T>(IBlobId projectionId, DateTimeOffset timestamp) where T : IProjectionDefinition
        => Task.FromResult(new ReadResult<T>(default));

    public Task<ReadResult<IProjectionDefinition>> GetAsync(IBlobId projectionId)
        => Task.FromResult(Result);

    public Task<ReadResult<IProjectionDefinition>> GetAsOfAsync(IBlobId projectionId, DateTimeOffset timestamp)
        => Task.FromResult(Result);

    public Task<ReadResult<IProjectionDefinition>> GetAsync(IBlobId projectionId, Type projectionType)
        => Task.FromResult(Result);

    public Task<ReadResult<IProjectionDefinition>> GetAsOfAsync(IBlobId projectionId, Type projectionType, DateTimeOffset timestamp)
        => Task.FromResult(Result);
}

public class FakeProjectionStore : IProjectionStore
{
    public Task EnumerateProjectionsAsync(ProjectionsOperator @operator, ProjectionQueryOptions options) => Task.CompletedTask;

    public Task SaveAsync(ProjectionCommit commit) => Task.CompletedTask;
}

public class FakeSerializer : ISerializer
{
    public byte[] SerializeToBytes<T>(T message) => Array.Empty<byte>();

    public string SerializeToString<T>(T message) => string.Empty;

    public T DeserializeFromBytes<T>(byte[] bytes) => default;
}

public class FakeOptionsMonitor<T> : IOptionsMonitor<T>
{
    public FakeOptionsMonitor(T value)
    {
        Value = value;
    }

    public T CurrentValue => Value;

    public T Value { get; }

    public T Get(string name) => Value;

    public IDisposable OnChange(Action<T, string> listener) => new NoopDisposable();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

public class FakeContextAccessor : IInceptionContextAccessor
{
    public FakeContextAccessor(string tenant)
    {
        Context = new InceptionContext(tenant, new ServiceCollection().BuildServiceProvider());
    }

    public InceptionContext Context { get; set; }
}

public class RecordingDangerZone : IDangerZone
{
    public List<string> Wiped { get; } = new();

    public Task WipeDataAsync(string tenant)
    {
        Wiped.Add(tenant);
        return Task.CompletedTask;
    }
}

public class FakeIndexType : IEventStoreIndex
{
    public Task IndexAsync(InceptionMessage message) => Task.CompletedTask;
}



public static class FacadeFactory
{
    public static (InceptionManagement management,
        FakeEventStore eventStore,
        FakeProjectionReader projectionReader,
        RecordingPublisher<IEvent> eventPublisher,
        RecordingPublisher<IPublicEvent> publicEventPublisher,
        RecordingPublisher<ICommand> commandPublisher,
        RecordingPublisher<ISystemSignal> signalPublisher,
        FakeContextAccessor contextAccessor,
        RecordingDangerZone dangerZone)
        Build(TypeContainer<IEventStoreIndex> indices = null)
    {
        var eventStore = new FakeEventStore();
        var projectionReader = new FakeProjectionReader();
        var projectionStore = new FakeProjectionStore();
        var contextAccessor = new FakeContextAccessor("tenant1");

        var eventStoreExplorer = new EventStoreExplorer(eventStore, new FakeOptionsMonitor<BoundedContext>(new BoundedContext { Name = "tenant1" }), new FakeSerializer());
        var projectionExplorer = new ProjectionExplorer(projectionReader, projectionStore, contextAccessor);

        var eventPublisher = new RecordingPublisher<IEvent>();
        var publicEventPublisher = new RecordingPublisher<IPublicEvent>();
        var commandPublisher = new RecordingPublisher<ICommand>();
        var signalPublisher = new RecordingPublisher<ISystemSignal>();
        var dangerZone = new RecordingDangerZone();

        var management = new InceptionManagement(
            eventStoreExplorer,
            projectionExplorer,
            eventPublisher,
            publicEventPublisher,
            commandPublisher,
            signalPublisher,
            contextAccessor,
            Options.Create(new TenantsOptions { Tenants = new[] { "tenant1", "tenant2" } }),
            indices ?? new TypeContainer<IEventStoreIndex>(new Type[0]),
            new ProjectionHasher(),
            new DangerZoneExecutor(contextAccessor, new FakeOptionsMonitor<DangerZoneOptions>(new DangerZoneOptions()), new IDangerZone[] { dangerZone }, NullLogger<DangerZoneExecutor>.Instance),
            new MonitorClient(new System.Net.Http.HttpClient()),
            NullLogger<InceptionManagement>.Instance);

        return (management, eventStore, projectionReader, eventPublisher, publicEventPublisher, commandPublisher, signalPublisher, contextAccessor, dangerZone);
    }
}

public static class EventStoreIndexStatusHelper
{
    public static EventStoreIndexStatus WithStatus(IndexStatus status)
    {
        var indexStatus = new EventStoreIndexStatus();
        indexStatus.State.Status = status;
        return indexStatus;
    }
}

#endregion
