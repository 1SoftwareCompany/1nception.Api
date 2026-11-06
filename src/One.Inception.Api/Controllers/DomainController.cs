using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace One.Inception.Api.Controllers;

[Route("domain")]
[AllowAnonymous]
public class DomainController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public DomainController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet, Route("explore")]
    public IActionResult Explore()
    {
        return management.ExploreDomain();
    }

    [HttpGet, Route("AggregateRootIdSamples")]
    public IActionResult GetAggregateRootIdSamples(string tenant)
    {
        return management.GetAggregateRootIdSamples(tenant);
    }

    [HttpGet, Route("tenants")]
    public IActionResult GetTenants()
    {
        return management.GetTenants();
    }
}
