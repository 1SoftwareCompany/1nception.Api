using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Projections")]
public class ProjectionListController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ProjectionListController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        return await management.ListProjections().ConfigureAwait(false);
    }
}
