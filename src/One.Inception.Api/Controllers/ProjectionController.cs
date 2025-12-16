using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionController : ApiControllerBase
{
    private readonly ProjectionExplorer _projectionExplorer;

    public ProjectionController(ProjectionExplorer projectionExplorer)
    {
        if (projectionExplorer is null) throw new ArgumentNullException(nameof(projectionExplorer));

        _projectionExplorer = projectionExplorer;
    }

    [HttpGet, Route("Explore")]
    public async Task<IActionResult> ExploreAsync([FromQuery] RequestModel model)
    {
        var projectionType = model.ProjectionName.GetTypeByContract();
        IBlobId id = GetId(model.Id);
        ProjectionDto result = await _projectionExplorer.ExploreAsync(id, projectionType, model.AsOf).ConfigureAwait(false);
        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
    }

    [HttpGet, Route("ExploreEvents")]
    public async Task<IActionResult> ExploreEvents([FromQuery] RequestModel model)
    {
        var projectionType = model.ProjectionName.GetTypeByContract();
        IBlobId id = GetId(model.Id);
        ProjectionDto result = await _projectionExplorer.ExploreIncludingEventsAsync(id, projectionType, model.AsOf).ConfigureAwait(false);
        result.State = null;

        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
    }

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

    public class RequestModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string ProjectionName { get; set; }

        public DateTimeOffset? AsOf { get; set; }
    }
}

public class BlobIdForSearch : IBlobId
{
    public BlobIdForSearch(byte[] bytes)
    {
        RawId = bytes;
    }

    public ReadOnlyMemory<byte> RawId { get; set; }
}
