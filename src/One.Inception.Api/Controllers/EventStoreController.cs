using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("EventStore")]
public class EventStoreController : ApiControllerBase
{
    private readonly InceptionManagement management;

    public EventStoreController(InceptionManagement management)
    {
        if (management is null) throw new ArgumentNullException(nameof(management));
        this.management = management;
    }

    [HttpGet, Route("Explore")]
    public async Task<IActionResult> Explore([FromQuery] EventStoreRequestModel model)
    {
        return await management.ExploreAggregate(model).ConfigureAwait(false);
    }

    [HttpPost, Route("ExploreWithPaging")]
    public async Task<IActionResult> ExploreWithPaging([FromBody] ExploreEventStoreWithPagingRequestModel model)
    {
        return await management.ExploreAggregateWithPaging(model).ConfigureAwait(false);
    }

    [HttpPost, Route("Republish")]
    public async Task<IActionResult> Republish([FromBody] RepublishRequest model)
    {
        return await management.Republish(model).ConfigureAwait(false);
    }

    [HttpPost]
    [Route("RepublishNew")]
    public async Task<IActionResult> RepublishNew([FromBody] RepublishRequestNew model)
    {
        return await management.RepublishNew(model).ConfigureAwait(false);
    }
}
