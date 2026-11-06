using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ProjectionController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet, Route("Explore")]
    public async Task<IActionResult> ExploreAsync([FromQuery] ProjectionRequestModel model)
    {
        return await management.ExploreProjection(model).ConfigureAwait(false);
    }

    [HttpGet, Route("ExploreEvents")]
    public async Task<IActionResult> ExploreEvents([FromQuery] ProjectionRequestModel model)
    {
        return await management.ExploreProjectionEvents(model).ConfigureAwait(false);
    }
}
