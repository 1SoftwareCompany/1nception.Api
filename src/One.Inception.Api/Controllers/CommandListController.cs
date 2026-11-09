using Microsoft.AspNetCore.Mvc;
using System;

namespace One.Inception.Api.Controllers;

[Route("Commands")]
public class CommandController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public CommandController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet]
    public IActionResult List()
    {
        return management.ListCommands();
    }
}
