using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("GetLiveBoundedContexts")]
public class RecipientBoundedContextController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public RecipientBoundedContextController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }
    [HttpGet]
    public async Task<IActionResult> GetRecipientBoundedContext()
    {
        return await management.GetRecipientBoundedContexts().ConfigureAwait(false);
    }
}
