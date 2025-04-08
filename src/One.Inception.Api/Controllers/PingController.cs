using Microsoft.AspNetCore.Mvc;

namespace One.Inception.Api.Controllers;

[Route("ping")]
public class PingController : ApiControllerBase
{
    [HttpGet]
    public IActionResult Ping()
    {
        return new OkObjectResult("pong");
    }
}
