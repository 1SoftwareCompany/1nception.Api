using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using One.Inception.Projections;
using System;
using System.Collections.Generic;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace One.Inception.Api;

public static class EventExtensions
{
    public static ProjectionCommitDto ToProjectionDto(this ProjectionCommit commit)
    {
        return new ProjectionCommitDto()
        {
            Events = new List<EventDto> { commit.Event.ToProjectionEventDto(commit.Event.Timestamp) },
            Timestamp = commit.Event.Timestamp
        };
    }

    public static ProjectionCommitDto ToProjectionDto(this IEvent @event)
    {
        return new ProjectionCommitDto()
        {
            Events = new List<EventDto> { @event.ToProjectionEventDto(@event.Timestamp) },
            Timestamp = @event.Timestamp
        };
    }

    public static EventDto ToProjectionEventDto(this IEvent @event, DateTimeOffset dateTimeOffset)
    {
        return @event.ToEventDto(dateTimeOffset, -1); // WHY -1 ?!? => Projections do not use positions to order events but TS.
    }

    public static IEnumerable<EventDto> ToEventDto(this IEnumerable<IEvent> events, DateTimeOffset timestamp)
    {
        int eventPosition = 0;
        foreach (IEvent @event in events)
        {
            yield return @event.ToEventDto(timestamp, eventPosition);
            eventPosition++;
        }
    }

    public static EventDto ToEventDto(this IEvent @event, DateTimeOffset timestamp, int position)
    {
        var entityEvent = @event as EntityEvent;
        if (entityEvent is null)
        {
            return new EventDto()
            {
                Id = @event.GetType().GetContractId(),
                EventName = @event.GetType().Name,
                EventData = @event,
                EventPosition = position,
                IsPublicEvent = typeof(IPublicEvent).IsAssignableFrom(@event.GetType()),
                Timestamp = timestamp
            };
        }
        else
        {
            return new EventDto()
            {
                Id = entityEvent.Event.GetType().GetContractId(),
                EventName = entityEvent.Event.GetType().Name,
                EventData = entityEvent.Event,
                IsEntityEvent = true,
                EventPosition = 1, // Why 1?
                IsPublicEvent = typeof(IPublicEvent).IsAssignableFrom(@event.GetType()),
                Timestamp = timestamp
            };
        }
    }

    public static RawEventDto ToRawEventDto(this IMessage @event, DateTimeOffset timestamp, int position, int revision)
    {
        var rawEvent = new RawEventDto();

        var entityEvent = @event as EntityEvent;

        if (entityEvent is null)
        {
            rawEvent = new RawEventDto()
            {
                Id = @event.GetType().GetContractId(),
                EventName = @event.GetType().Name,
                EventData = new EventDataWithMessageId(@event, @event.GetMessageId()),
                IsEntityEvent = false,
                EventPosition = position,
                EventRevision = revision,
                IsPublicEvent = typeof(IPublicEvent).IsAssignableFrom(@event.GetType()),
                Timestamp = timestamp
            };
        }
        else
        {
            rawEvent = new RawEventDto()
            {
                Id = entityEvent.Event.GetType().GetContractId(),
                EventName = entityEvent.Event.GetType().Name,
                EventData = new EventDataWithMessageId(entityEvent.Event, entityEvent.GetMessageId()),
                IsEntityEvent = true,
                EventPosition = position,
                EventRevision = revision,
                IsPublicEvent = typeof(IPublicEvent).IsAssignableFrom(@event.GetType()),
                Timestamp = timestamp
            };
        }

        return rawEvent;
    }

    public static IEnumerable<EventDto> ToPublicEventDto(this IEnumerable<IPublicEvent> events, int lastEventPosition)
    {
        int eventPosition = lastEventPosition + 5;
        foreach (IPublicEvent @event in events)
        {
            yield return new EventDto()
            {
                Id = @event.GetType().GetContractId(),
                EventName = @event.GetType().Name,
                EventData = @event,
                EventPosition = eventPosition,
                IsPublicEvent = true
            };

            eventPosition++;
        }
    }
}


[JsonConverter(typeof(EventDataWithMessageIdConverter))]
public sealed class EventDataWithMessageId
{
    public EventDataWithMessageId(object @event, string? messageId)
    {
        Event = @event;
        MessageId = messageId;
    }

    public object Event { get; }
    public string MessageId { get; }
}

public sealed class EventDataWithMessageIdConverter : JsonConverter<EventDataWithMessageId>
{
    public override bool CanRead => false;

    public override void WriteJson(JsonWriter writer, EventDataWithMessageId? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        // 'serializer' is MVC's serializer, so rawId etc. come out exactly as before
        var obj = JObject.FromObject(value.Event, serializer);
        obj["messageId"] = value.MessageId;
        obj.WriteTo(writer);
    }

    public override EventDataWithMessageId ReadJson(JsonReader reader, Type objectType, EventDataWithMessageId? existingValue, bool hasExistingValue, JsonSerializer serializer)
        => throw new NotSupportedException();
}
