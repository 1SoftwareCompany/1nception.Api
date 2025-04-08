using One.Inception.MessageProcessing;
using One.Inception.Projections;
using One.Inception.Projections.Versioning;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionCancelController : ApiControllerBase
{
    private readonly IPublisher<ICommand> _publisher;
    private readonly IInceptionContextAccessor contextAccessor;

    public ProjectionCancelController(IPublisher<ICommand> publisher, IInceptionContextAccessor contextAccessor)
    {
        if (publisher is null) throw new ArgumentNullException(nameof(publisher));
        if (contextAccessor is null) throw new ArgumentNullException(nameof(contextAccessor));

        _publisher = publisher;
        this.contextAccessor = contextAccessor;
    }

    [HttpPost, Route("Pause")]
    public IActionResult Pause([FromBody] ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new PauseProjectionVersion(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version);

        if (_publisher.Publish(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(NewProjectionVersion)}'"));
    }

    [HttpPost, Route("Cancel")]
    public IActionResult Cancel([FromBody] ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new CancelProjectionVersionRequest(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version, model.Reason ?? "Canceled by user");

        if (_publisher.Publish(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(CancelProjectionVersionRequest)}'"));
    }

    [HttpPost, Route("Finalize")]
    public IActionResult Finalize([FromBody] ProjcetionRequestModel model)
    {
        var version = new Projections.ProjectionVersion(model.ProjectionContractId, ProjectionStatus.Create(model.Version.Status), model.Version.Revision, model.Version.Hash);
        var command = new FinalizeProjectionVersionRequest(new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant), version);

        if (_publisher.Publish(command))
            return new OkObjectResult(new ResponseResult());

        return new BadRequestObjectResult(new ResponseResult<string>($"Unable to publish command '{nameof(FinalizeProjectionVersionRequest)}'"));
    }

    public class ProjcetionRequestModel
    {
        [Required]
        public string ProjectionContractId { get; set; }

        [Required]
        public ProjectionVersionDto Version { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}
