using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using One.Inception.Discoveries;
using One.Inception.MessageProcessing;
using One.Inception.Projections;
using One.Inception.Projections.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace One.Inception.Api.Controllers;

[Route("Projection")]
public class ProjectionMetaController : ApiControllerBase
{
    private readonly ProjectionExplorer _projectionExplorer;
    private readonly IInceptionContextAccessor contextAccessor;
    private readonly ProjectionHasher projectionHasher;

    public ProjectionMetaController(ProjectionExplorer projectionExplorer, IInceptionContextAccessor contextAccessor, ProjectionHasher projectionHasher)
    {
        if (projectionExplorer is null) throw new ArgumentNullException(nameof(projectionExplorer));

        _projectionExplorer = projectionExplorer;
        this.contextAccessor = contextAccessor;
        this.projectionHasher = projectionHasher;
    }

    [HttpGet, Route("Meta")]
    public async Task<IActionResult> Meta([FromQuery] RequestModel model)
    {
        IEnumerable<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);
        Type metadata = loadedAssemblies
            .SelectMany(assembly => assembly.GetLoadableTypes()
            .Where(x => typeof(IProjection).IsAssignableFrom(x))
            .Where(x => x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0))
            .Where(x => x.GetContractId() == model.ProjectionContractId)
            .FirstOrDefault();

        if (metadata is null) return new BadRequestObjectResult(new ResponseResult<string>($"Projection with contract '{model.ProjectionContractId}' not found"));

        var id = new ProjectionVersionManagerId(model.ProjectionContractId, contextAccessor.Context.Tenant);
        ProjectionDto dto = await _projectionExplorer.ExploreAsync(id, typeof(ProjectionVersionsHandler));
        var state = dto?.State as ProjectionVersionsHandlerState;

        ProjectionAttribute contract = metadata
            .GetCustomAttributes(true).Where(attr => attr is ProjectionAttribute)
            .SingleOrDefault() as ProjectionAttribute;

        var metaProjection = new ProjectionMeta()
        {
            ProjectionContractId = metadata.GetContractId(),
            ProjectionName = metadata.Name,
            IsReplayable = contract is not null,
            IsRebuildable = contract is not null && contract.Persistence == ProjectionEventsPersistenceSetting.Persistent, // why would you want a new version for not persisted projection, only fixing is allowed
            IsSearchable = typeof(IProjectionDefinition).IsAssignableFrom(metadata)
        };

        if (state is null)
        {
            metaProjection.Versions.Add(new ProjectionVersionDto()
            {
                Status = ProjectionStatus.NotPresent,
                Hash = projectionHasher.CalculateHash(typeof(ProjectionVersionsHandler)),
                Revision = 0
            });
        }
        else
        {
            foreach (var ver in state.AllVersions)
            {
                metaProjection.Versions.Add(new ProjectionVersionDto()
                {
                    Hash = ver.Hash,
                    Revision = ver.Revision,
                    Status = ver.Status
                });
            }
        }

        return Ok(new ResponseResult<ProjectionMeta>(metaProjection));
    }

    public class RequestModel
    {
        [Required]
        public string ProjectionContractId { get; set; }
    }
}
