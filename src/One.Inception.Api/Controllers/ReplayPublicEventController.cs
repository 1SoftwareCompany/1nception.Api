using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

public class ReplayPublicEventController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public ReplayPublicEventController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpPost, Route("ReplayPublicEvent")]
    public async Task<IActionResult> ReplayPublicEvent([FromBody] ReplayPublicEventRequest model)
    {
        return await management.ReplayPublicEvent(model).ConfigureAwait(false);
    }
}
