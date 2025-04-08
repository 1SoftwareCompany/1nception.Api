using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using One.Inception.DangerZone;

namespace One.Inception.Api.Controllers;

[Route("DangerZone")]
public class DangerZoneController : ApiControllerBase
{
    private readonly DangerZoneExecutor _dangerZoneExecutor;

    public DangerZoneController(DangerZoneExecutor dangerZoneExecutor)
    {
        if (dangerZoneExecutor is null) throw new ArgumentNullException(nameof(dangerZoneExecutor));
        _dangerZoneExecutor = dangerZoneExecutor;
    }

    [HttpDelete, Route("Wipe")]
    public async Task<IActionResult> WipeAsync([FromQuery, Required] string tenant)
    {
        await _dangerZoneExecutor.WipeDataAsync(tenant).ConfigureAwait(false);

        return new OkObjectResult(new ResponseResult());
    }
}
