using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Indices")]
public class IndexListController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public IndexListController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        return await management.ListIndices().ConfigureAwait(false);
    }
}
