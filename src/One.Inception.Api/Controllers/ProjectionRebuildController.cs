using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionRebuildController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ProjectionRebuildController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpPost, Route("Fix"), Route("Rebuild")]
    public async Task<IActionResult> Fix([FromBody] ProjectionRebuildRequestModel model)
    {
        return await management.FixProjectionVersion(model).ConfigureAwait(false);
    }

    [HttpPost, Route("New"), Route("Replay")]
    public async Task<IActionResult> New([FromBody] ProjectionRebuildRequestModel model)
    {
        return await management.NewProjectionVersion(model).ConfigureAwait(false);
    }
}
