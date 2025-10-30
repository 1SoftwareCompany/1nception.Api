using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using One.Inception.EventStore.Index;
using One.Inception.EventStore.Index.Handlers;
using One.Inception.MessageProcessing;
using Microsoft.AspNetCore.Mvc;

namespace One.Inception.Api.Controllers;

[Route("EventStore/Index")]
public class EventStoreIndexController : ApiControllerBase
{
    private readonly TypeContainer<IEventStoreIndex> endicesTypes;
    private readonly IPublisher<ICommand> publisher;
    private readonly ProjectionExplorer projection;
    private readonly IInceptionContextAccessor contextAccessor;

    public EventStoreIndexController(IInceptionContextAccessor contextAccessor, TypeContainer<IEventStoreIndex> endicesTypes, IPublisher<ICommand> publisher, ProjectionExplorer projection)
    {
        this.contextAccessor = contextAccessor;
        this.endicesTypes = endicesTypes;
        this.publisher = publisher;
        this.projection = projection;
    }

    [HttpGet, Route("Meta")]
    public async Task<IActionResult> Meta()
    {
        List<MetaResponseModel> result = new List<MetaResponseModel>();

        foreach (var index in endicesTypes.Items)
        {
            var status = await projection.ExploreAsync(new EventStoreIndexManagerId(index.GetContractId(), contextAccessor.Context.Tenant), typeof(EventStoreIndexStatus));

            MetaResponseModel indexResponse = new MetaResponseModel()
            {
                Id = index.GetContractId(),
                Name = index.Name,
                Status = status.State.ToString()
            };

            result.Add(indexResponse);
        }

        return new OkObjectResult(new ResponseResult<List<MetaResponseModel>>(result));
    }

    [HttpPost, Route("Rebuild")]
    public async Task<IActionResult> Rebuild([FromBody] RebuildIndexRequestModel model)
    {
        var command = new RebuildIndexCommand(new EventStoreIndexManagerId(model.Id, contextAccessor.Context.Tenant), model.MaxDegreeOfParallelism);

        if (await publisher.PublishAsync(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(RebuildIndexCommand)}'"));
    }

    public class MetaResponseModel
    {
        public MetaResponseModel()
        {
            Status = IndexStatus.NotPresent;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Status { get; set; }
    }

    public class RebuildIndexRequestModel
    {
        [Required]
        public string Id { get; set; }

        public int? MaxDegreeOfParallelism { get; set; }
    }
}
