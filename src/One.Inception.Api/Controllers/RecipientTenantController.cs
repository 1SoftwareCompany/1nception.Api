using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("GetTenantContexts")]
public class RecipientTenantController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public RecipientTenantController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }
    [HttpGet]
    public async Task<IActionResult> GetTenantContext()
    {
        return await management.GetTenantContexts().ConfigureAwait(false);
    }
}
