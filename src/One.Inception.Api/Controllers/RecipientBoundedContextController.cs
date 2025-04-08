using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("GetLiveBoundedContexts")]
public class RecipientBoundedContextController : ApiControllerBase
{
    private readonly MonitorClient monitorClient;

    public RecipientBoundedContextController(MonitorClient monitorClient)
    {
        this.monitorClient = monitorClient;
    }
    [HttpGet]
    public async Task<IActionResult> GetRecipientBoundedContext()
    {
        List<string> liveBoundedContext = await monitorClient.GetBoundedContextListAsync().ConfigureAwait(false);

        return Ok(liveBoundedContext);
    }
}
