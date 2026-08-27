using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using One.Inception.EventStore.Players;
using static One.Inception.Api.Controllers.DomainController;

namespace One.Inception.Api.Controllers;

public class ReplayEventController : ApiControllerBase
{
    private readonly IPublisher<ISystemSignal> signalPublisher;
    private DateTimeOffset ReplayAfterDefaultDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));
    private DateTimeOffset ReplayBeforeDefaultDate = new DateTimeOffset(2100, 1, 1, 0, 0, 0, 0, TimeSpan.FromHours(0));

    public ReplayEventController(IPublisher<ISystemSignal> signalPublisher)
    {
        this.signalPublisher = signalPublisher;
    }

    [HttpPost, Route("ReplayEvent")]
    public async Task<IActionResult> ReplayEvent([FromBody] ReplayEventRequest model)
    {
        if (model.ReplayAfter.HasValue)
            ReplayAfterDefaultDate = model.ReplayAfter.Value;

        if (model.ReplayBefore.HasValue)
            ReplayBeforeDefaultDate = model.ReplayBefore.Value;

        ICollection<Event_Response_New> allEvents = EventCollectionExtensions.GetAndCacheEvents();

        Event_Response_New found = allEvents.Where(x => x.Id.Equals(model.SourceEventTypeId)).SingleOrDefault();
        if (found is null)
            return new BadRequestObjectResult(new ResponseResult<string>($"Unable to start replay. This event is not found.", $"Unable to start replay. This event is not found."));

        Urn urn = null;
        if (string.IsNullOrEmpty(model.AggregateId) == false)
            urn = new Urn(model.AggregateId);

        if (found.IsPublicEvent)
        {
            if (string.IsNullOrEmpty(found.BC))
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'. BC of public event not found.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'. BC of public event not found."));

            if (model.RecipientBoundedContext.Equals(found.BC, StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish public event in the same bounded context.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish public event in the same bounded context."));

            var replay = new ReplayPublicEventsRequested()
            {
                Tenant = model.Tenant,
                RecipientBoundedContext = model.RecipientBoundedContext,
                RecipientHandlers = model.RecipientHandlers,
                SourceEventTypeId = model.SourceEventTypeId,
                ReplayOptions = new ReplayEventsOptionNew()
                {
                    After = ReplayAfterDefaultDate,
                    Before = ReplayBeforeDefaultDate,
                    AggregateRootId = urn,
                    ShouldReplayLastEventOnly = model.ShouldReplayLastEventOnly,
                },
            };

            if (await signalPublisher.PublishAsync(replay))
                return new OkObjectResult(new ResponseResult());
        }
        else
        {
            if (string.IsNullOrEmpty(found.BC) || model.RecipientBoundedContext.Equals(found.BC, StringComparison.OrdinalIgnoreCase))
            {
                var replay = new ReplayInternalEventsRequested()
                {
                    Tenant = model.Tenant,
                    RecipientBoundedContext = model.RecipientBoundedContext,
                    RecipientHandlers = model.RecipientHandlers,
                    SourceEventTypeId = model.SourceEventTypeId,
                    ReplayOptions = new ReplayEventsOptionNew()
                    {
                        After = ReplayAfterDefaultDate,
                        Before = ReplayBeforeDefaultDate,
                        AggregateRootId = urn,
                        ShouldReplayLastEventOnly = model.ShouldReplayLastEventOnly,
                    },
                };

                if (await signalPublisher.PublishAsync(replay))
                    return new OkObjectResult(new ResponseResult());
            }
            else
            {
                return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish internal event in another bounded context.", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'.Can not publish internal event in another bounded context."));
            }
        }

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish '{nameof(ReplayPublicEventsRequested)}'", $"Unable to publish '{nameof(ReplayPublicEventsRequested)}'"));
    }
}


public class ReplayEventRequest
{
    [Required]
    public string Tenant { get; set; }

    [Required]
    public string RecipientBoundedContext { get; set; }

    [Required]
    public string RecipientHandlers { get; set; }

    [Required]
    public string SourceEventTypeId { get; set; }

    public bool ShouldReplayLastEventOnly { get; set; } // default behaviour will be false

    public string AggregateId { get; set; } // can be optional

    public DateTimeOffset? ReplayAfter { get; set; }

    public DateTimeOffset? ReplayBefore { get; set; }
}
