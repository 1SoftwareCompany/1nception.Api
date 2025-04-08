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
        ProjectionDto result = await _projectionExplorer.ExploreAsync(new Urn(model.Id), projectionType, model.AsOf).ConfigureAwait(false);
        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
    }

    [HttpGet, Route("ExploreEvents")]
    public async Task<IActionResult> ExploreEvents([FromQuery] RequestModel model)
    {
        var projectionType = model.ProjectionName.GetTypeByContract();
        ProjectionDto result = await _projectionExplorer.ExploreIncludingEventsAsync(new Urn(model.Id), projectionType, model.AsOf).ConfigureAwait(false);
        result.State = null;

        return new OkObjectResult(new ResponseResult<ProjectionDto>(result));
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
