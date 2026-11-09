using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

public class ReplayEventController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ReplayEventController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpPost, Route("ReplayEvent")]
    public async Task<IActionResult> ReplayEvent([FromBody] ReplayEventRequest model)
    {
        return await management.ReplayEvent(model).ConfigureAwait(false);
    }
}
