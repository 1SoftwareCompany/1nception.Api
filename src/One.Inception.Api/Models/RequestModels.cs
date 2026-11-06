using System;
using System.ComponentModel.DataAnnotations;
using One.Inception.EventStore.Players;
using One.Inception.Projections.Versioning;

namespace One.Inception.Api;

/// <summary>
/// Request models for the management functionality of <see cref="InceptionManagement"/>.
/// They were previously declared inside the controllers of the HTTP API.
/// </summary>

/// <summary>
/// A request for exploring an aggregate of an event store. (was <c>EventStoreController.RequestModel</c>)
/// </summary>
public class EventStoreRequestModel
{
    [Required]
    public string Id { get; set; }
}

/// <summary>
/// A request for exploring a projection. (was <c>ProjectionController.RequestModel</c>)
/// </summary>
public class ProjectionRequestModel
{
    [Required]
    public string Id { get; set; }

    [Required]
    public string ProjectionName { get; set; }

    public DateTimeOffset? AsOf { get; set; }
}

/// <summary>
/// A request for getting the metadata of a projection. (was <c>ProjectionMetaController.RequestModel</c>)
/// </summary>
public class ProjectionMetaRequestModel
{
    [Required]
    public string ProjectionContractId { get; set; }
}

/// <summary>
/// A request for fixing or creating a new version of a projection. (was <c>ProjectionRebuildController.RequestModel</c>)
/// </summary>
public class ProjectionRebuildRequestModel
{
    public ProjectionRebuildRequestModel()
    {
        PlayerOptions = new PlayerOptions();
    }

    [Required]
    public string ProjectionContractId { get; set; }

    [Required]
    public string Hash { get; set; }

    public PlayerOptions PlayerOptions { get; set; }
}

/// <summary>
/// Options for configuring the player of a projection. (was nested in <c>ProjectionRebuildController.RequestModel</c>)
/// </summary>
public class PlayerOptions
{
    public DateTimeOffset? After { get; set; }

    public DateTimeOffset? Before { get; set; }

    [Range(1, 10_000)]
    public int? MaxDegreeOfParallelism { get; set; }
}

/// <summary>
/// A request for republishing a single event of an aggregate.
/// </summary>
public class RepublishRequest
{
    [Required]
    public string[] RecipientHandlers { get; set; }

    [Required]
    public string Id { get; set; }

    [Required]
    public int CommitRevision { get; set; }

    [Required]
    public int EventPosition { get; set; }

    public bool IsPublicEvent { get; set; }
}

/// <summary>
/// A request for republishing a single event of an aggregate, based on its raw event record.
/// </summary>
public class RepublishRequestNew
{
    [Required]
    public string[] RecipientHandlers { get; set; }

    [Required]
    public string Id { get; set; }

    [Required]
    public int CommitRevision { get; set; }

    [Required]
    public int EventPosition { get; set; }

    public bool IsPublicEvent { get; set; }

    [Required]
    public long Timestamp { get; set; }

    [Required]
    public string EventContract { get; set; }
}

/// <summary>
/// A request for exploring an event store with paging.
/// </summary>
public class ExploreEventStoreWithPagingRequestModel
{
    [Required]
    public string Id { get; set; }

    public byte[] PaginationToken { get; set; }

    public int Take { get; set; }
}

/// <summary>
/// A request for replaying a single event to a bounded context.
/// </summary>
public class ReplayEventRequest
{
    [Required]
    public string Tenant { get; set; }

    [Required]
    public string RecipientBoundedContext { get; set; }

    [Required]
    public string RecipientHandlers { get; set; }

    [Required]
    public string SourceEventTypeId { get; set; }

    public bool ShouldReplayLastEventOnly { get; set; } // default behaviour will be false

    public string AggregateId { get; set; } // can be optional

    public DateTimeOffset? ReplayAfter { get; set; }

    public DateTimeOffset? ReplayBefore { get; set; }
}

/// <summary>
/// A request for replaying a single public event to a bounded context.
/// </summary>
public class ReplayPublicEventRequest
{
    [Required]
    public string Tenant { get; set; }

    [Required]
    public string RecipientBoundedContext { get; set; }

    [Required]
    public string RecipientHandlers { get; set; }

    [Required]
    public string SourceEventTypeId { get; set; }

    public DateTimeOffset? ReplayAfter { get; set; }

    public DateTimeOffset? ReplayBefore { get; set; }
}

/// <summary>
/// A request for pausing, cancelling or finalizing a version of a projection.
/// The type name is kept as-is from the original controller (typo included) to preserve the public surface.
/// </summary>
public class ProjcetionRequestModel
{
    [Required]
    public string ProjectionContractId { get; set; }

    [Required]
    public ProjectionVersionDto Version { get; set; }

    [Required]
    public string Reason { get; set; }
}

/// <summary>
/// A request for rebuilding the whole event store index.
/// </summary>
public class RebuildIndexRequestModel
{
    [Required]
    public string Id { get; set; }

    public int? MaxDegreeOfParallelism { get; set; }
}

/// <summary>
/// A request for rebuilding or finalizing a specific index.
/// </summary>
public class IndexRequestModel
{
    [Required]
    public string IndexContractId { get; set; }

    public int? MaxDegreeOfParallelism { get; set; }
}
