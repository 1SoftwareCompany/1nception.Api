using System.Collections.Generic;

namespace One.Inception.Api;

/// <summary>
/// Response models for exploring the domain model.
/// They were previously declared inside the <c>DomainController</c> of the HTTP API.
/// </summary>

/// <summary>
/// The base of all domain model responses.
/// </summary>
public class BaseDomainModel_Response
{
    public string Id { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// A base for domain model responses that have a contract id.
/// </summary>
public class BaseSerializableDomainModel_Response : BaseDomainModel_Response
{
}

/// <summary>
/// A response describing the whole domain model.
/// </summary>
public class Domain_Response
{
    public Domain_Response()
    {
        Aggregates = new List<Aggregate_Response>();
    }

    public IEnumerable<Aggregate_Response> Aggregates { get; set; }

    public IEnumerable<AggregateIdSample_Response> AggregateIdSamples { get; set; }

    public IEnumerable<Event_Response> Events { get; set; }

    public IEnumerable<Command_Response> Commands { get; set; }

    public IEnumerable<Projection_Response> Projections { get; set; }

    public IEnumerable<Saga_Response> Sagas { get; set; }

    public IEnumerable<Port_Response> Ports { get; set; }

    public IEnumerable<Gateway_Response> Gateways { get; set; }
}

/// <summary>
/// A response describing an aggregate root.
/// </summary>
public class Aggregate_Response : BaseDomainModel_Response
{
    public IEnumerable<Event_Response> Events { get; set; }

    public IEnumerable<Command_Response> Commands { get; set; }
}

/// <summary>
/// A response with a sample of an aggregate root id of a tenant.
/// </summary>
public class AggregateIdSample_Response
{
    public string IdSample { get; set; }
}

/// <summary>
/// A response describing an event.
/// </summary>
public class Event_Response : BaseSerializableDomainModel_Response
{
    public Event_Response()
    {
        Properties = new List<string>();
    }

    public List<string> Properties { get; set; }

    public bool IsPublicEvent { get; set; }
}

/// <summary>
/// A response describing a command.
/// </summary>
public class Command_Response : BaseSerializableDomainModel_Response
{
}

/// <summary>
/// A response describing a projection.
/// </summary>
public class Projection_Response : BaseSerializableDomainModel_Response
{
    public bool IsEventSourced { get; set; }

    public IEnumerable<Event_Response> Events { get; set; }
}

/// <summary>
/// A response describing a saga (process manager).
/// </summary>
public class Saga_Response : BaseDomainModel_Response
{
    public IEnumerable<Event_Response> Events { get; set; }
}

/// <summary>
/// A response describing a port.
/// </summary>
public class Port_Response : BaseDomainModel_Response
{
    public IEnumerable<Event_Response> Events { get; set; }
}

/// <summary>
/// A response describing a gateway.
/// </summary>
public class Gateway_Response : BaseDomainModel_Response
{
    public IEnumerable<Event_Response> Events { get; set; }
}

/// <summary>
/// A response describing an event, including its bounded context.
/// Used for the replay functionality. (was in <c>EventCollectionExtensions.cs</c>)
/// </summary>
public class Event_Response_New
{
    public Event_Response_New()
    {
        Properties = new List<string>();
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string BC { get; set; }

    public bool IsPublicEvent { get; set; }

    public List<string> Properties { get; set; }
}
