using Microsoft.AspNetCore.Mvc;
using System;

namespace One.Inception.Api.Controllers;

[Route("ping")]
public class PingController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public PingController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet]
    public IActionResult Ping()
    {
        return management.Ping();
    }
}
