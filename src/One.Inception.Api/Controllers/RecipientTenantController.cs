using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("GetTenantContexts")]
public class RecipientTenantController : ApiControllerBase
{
    private readonly MonitorClient monitorClient;

    public RecipientTenantController(MonitorClient monitorClient)
    {
        this.monitorClient = monitorClient;
    }
    [HttpGet]
    public async Task<IActionResult> GetTenantContext()
    {
        List<string> liveTenant = await monitorClient.GetTenantListAsync().ConfigureAwait(false);

        return Ok(liveTenant);
    }
}
