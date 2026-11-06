using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Index")]
public class IndexRebuildActionsController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public IndexRebuildActionsController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpPost, Route("Rebuild")]
    public async Task<IActionResult> Rebuild([FromBody] IndexRequestModel model)
    {
        return await management.RebuildIndex(model).ConfigureAwait(false);
    }

    [HttpPost, Route("Finalize")]
    public async Task<IActionResult> Finalize([FromBody] IndexRequestModel model)
    {
        return await management.FinalizeIndex(model).ConfigureAwait(false);
    }
}
