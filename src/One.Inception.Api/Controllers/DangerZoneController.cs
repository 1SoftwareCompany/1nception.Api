using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("DangerZone")]
public class DangerZoneController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public DangerZoneController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpDelete, Route("Wipe")]
    public async Task<IActionResult> WipeAsync([FromQuery, Required] string tenant)
    {
        return await management.WipeAsync(tenant).ConfigureAwait(false);
    }
}
