using One.Inception.EventStore;
using One.Inception.EventStore.Index;
using System;
using System.Collections.Generic;

namespace One.Inception.Api;

/// <summary>
/// Response models for the explorer functionality.
/// They were previously declared inside the controllers of the HTTP API.
/// </summary>

/// <summary>
/// A list of commands.
/// </summary>
public class CommandListDto
{
    public CommandListDto()
    {
        Commands = new List<CommandMeta>();
    }

    public List<CommandMeta> Commands { get; set; }
}

/// <summary>
/// The metadata of a command.
/// </summary>
public class CommandMeta
{
    public string Type { get; set; }

    public string DataContract { get; set; }
}

/// <summary>
/// The metadata of an event store index.
/// </summary>
public class MetaResponseModel
{
    public MetaResponseModel()
    {
        Status = IndexStatus.NotPresent;
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string Status { get; set; }
}

/// <summary>
/// A list of event store indices.
/// </summary>
public class IndexListDto
{
    public IndexListDto()
    {
        Indices = new List<IndexMeta>();
    }

    public List<IndexMeta> Indices { get; set; }
}

/// <summary>
/// The metadata of an event store index.
/// </summary>
public class IndexMeta
{
    public string IndexContractId { get; set; }

    public string IndexName { get; set; }

    public string Status { get; set; }
}

/// <summary>
/// A list of projections with their versions.
/// </summary>
public class ProjectionListDto
{
    public ProjectionListDto()
    {
        Projections = new List<ProjectionMeta>();
    }

    public List<ProjectionMeta> Projections { get; set; }
}

/// <summary>
/// The metadata of a projection, including its versions.
/// </summary>
public class ProjectionMeta
{
    public ProjectionMeta()
    {
        Versions = new List<ProjectionVersionDto>();
    }

    public string ProjectionContractId { get; set; }

    public string ProjectionName { get; set; }

    public bool IsReplayable { get; set; }

    public bool IsRebuildable { get; set; }

    public bool IsSearchable { get; set; }

    public DateTimeOffset? AfterTimestampFromAttribute { get; set; }

    public List<ProjectionVersionDto> Versions { get; set; }
}

/// <summary>
/// A version of a projection.
/// </summary>
public class ProjectionVersionDto
{
    public string Hash { get; set; }

    public int Revision { get; set; }

    public string Status { get; set; }

    public string RebuildStatus { get; set; }
}

/// <summary>
/// An identifier of an entity, given as raw bytes (hex-encoded in the request string).
/// (was in <c>ProjectionController.cs</c>)
/// </summary>
public class BlobIdForSearch : IBlobId
{
    public BlobIdForSearch(byte[] bytes)
    {
        RawId = bytes;
    }

    public ReadOnlyMemory<byte> RawId { get; set; }
}
