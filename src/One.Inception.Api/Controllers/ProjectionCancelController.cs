using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionCancelController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ProjectionCancelController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpPost, Route("Pause")]
    public async Task<IActionResult> Pause([FromBody] ProjcetionRequestModel model)
    {
        return await management.PauseProjection(model).ConfigureAwait(false);
    }

    [HttpPost, Route("Cancel")]
    public async Task<IActionResult> Cancel([FromBody] ProjcetionRequestModel model)
    {
        return await management.CancelProjection(model).ConfigureAwait(false);
    }

    [HttpPost, Route("Finalize")]
    public async Task<IActionResult> Finalize([FromBody] ProjcetionRequestModel model)
    {
        return await management.FinalizeProjection(model).ConfigureAwait(false);
    }
}
