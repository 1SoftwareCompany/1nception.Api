using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("EventStore/Index")]
public class EventStoreIndexController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public EventStoreIndexController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet, Route("Meta")]
    public async Task<IActionResult> Meta()
    {
        return await management.GetEventStoreIndexMeta().ConfigureAwait(false);
    }

    [HttpPost, Route("Rebuild")]
    public async Task<IActionResult> Rebuild([FromBody] RebuildIndexRequestModel model)
    {
        return await management.RebuildEventStoreIndex(model).ConfigureAwait(false);
    }
}
