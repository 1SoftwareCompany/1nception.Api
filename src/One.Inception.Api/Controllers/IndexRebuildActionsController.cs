using System;
using System.ComponentModel.DataAnnotations;
using One.Inception.EventStore.Index;
using One.Inception.MessageProcessing;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace One.Inception.Api.Controllers;

[Route("Index")]
public class IndexRebuildActionsController : ApiControllerBase
{
    private readonly IPublisher<ICommand> _publisher;
    private readonly IInceptionContextAccessor contextAccessor;

    public IndexRebuildActionsController(IPublisher<ICommand> publisher, IInceptionContextAccessor contextAccessor)
    {
        if (publisher is null) throw new ArgumentNullException(nameof(publisher));

        _publisher = publisher;
        this.contextAccessor = contextAccessor;
    }

    [HttpPost, Route("Rebuild")]
    public async Task<IActionResult> Rebuild([FromBody] IndexRequestModel model)
    {
        var command = new RebuildIndexCommand(new EventStoreIndexManagerId(model.IndexContractId, contextAccessor.Context.Tenant), model.MaxDegreeOfParallelism);

        if (await _publisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'"));
    }

    [HttpPost, Route("Finalize")]
    public async Task<IActionResult> Finalize([FromBody] IndexRequestModel model)
    {
        var command = new FinalizeEventStoreIndexRequest(new EventStoreIndexManagerId(model.IndexContractId, contextAccessor.Context.Tenant));

        if (await _publisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeEventStoreIndexRequest)}'"));
    }

    public class IndexRequestModel
    {
        [Required]
        public string IndexContractId { get; set; }

        public int? MaxDegreeOfParallelism { get; set; }
    }
}
