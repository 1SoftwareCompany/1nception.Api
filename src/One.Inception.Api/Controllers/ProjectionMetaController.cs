using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionMetaController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ProjectionMetaController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet, Route("Meta")]
    public async Task<IActionResult> Meta([FromQuery] ProjectionMetaRequestModel model)
    {
        return await management.GetProjectionMeta(model).ConfigureAwait(false);
    }
}
