using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using One.Inception.Discoveries;
using One.Inception.MessageProcessing;
using One.Inception.Projections;
using One.Inception.Projections.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace One.Inception.Api.Controllers;

[Route("Projections")]
public class ProjectionListController : ApiControllerBase
{
    private readonly ProjectionExplorer _projectionExplorer;
    private readonly IInceptionContextAccessor contextAccessor;
    private readonly ProjectionHasher projectionHasher;

    public ProjectionListController(ProjectionExplorer projectionExplorer, IInceptionContextAccessor contextAccessor, ProjectionHasher projectionHasher)
    {
        if (projectionExplorer is null) throw new ArgumentNullException(nameof(projectionExplorer));
        if (contextAccessor is null) throw new ArgumentNullException(nameof(contextAccessor));

        _projectionExplorer = projectionExplorer;
        this.contextAccessor = contextAccessor;
        this.projectionHasher = projectionHasher;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        var projectionMetaData = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(x => typeof(IProjection).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0));

        ProjectionListDto result = new ProjectionListDto();
        foreach (var meta in projectionMetaData)
        {
            var id = new ProjectionVersionManagerId(meta.GetContractId(), contextAccessor.Context.Tenant);
            var dto = await _projectionExplorer.ExploreAsync(id, typeof(ProjectionVersionsHandler));
            ProjectionVersionsHandlerState state = dto?.State as ProjectionVersionsHandlerState;

            ProjectionAttribute contract = meta
                .GetCustomAttributes(true).Where(attr => attr is ProjectionAttribute)
                .SingleOrDefault() as ProjectionAttribute;

            var metaProjection = new ProjectionMeta()
            {
                ProjectionContractId = meta.GetContractId(),
                ProjectionName = meta.Name,
                IsReplayable = contract is not null,
                IsRebuildable = contract is not null && contract.Persistence == ProjectionEventsPersistenceSetting.Persistent, // why would you want a new version for not persisted projection, only fixing is allowed
                IsSearchable = typeof(IProjectionDefinition).IsAssignableFrom(meta),
                AfterTimestampFromAttribute = contract.Timestamp
            };
            if (ReferenceEquals(null, state))
            {
                metaProjection.Versions.Add(new ProjectionVersionDto()
                {
                    Status = ProjectionStatus.NotPresent,
                    Hash = projectionHasher.CalculateHash(meta),
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
            result.Projections.Add(metaProjection);
        }

        return new OkObjectResult(new ResponseResult<ProjectionListDto>(result));
    }
}

public class ProjectionListDto
{
    public ProjectionListDto()
    {
        Projections = new List<ProjectionMeta>();
    }

    public List<ProjectionMeta> Projections { get; set; }
}

public class ProjectionMeta
{
    public ProjectionMeta()
    {
        Versions = new List<ProjectionVersionDto>();
    }

    public string ProjectionContractId { get; set; }

    public string ProjectionName { get; set; }

    public bool IsReplayable { get; set; }

    public bool IsRebuildable { get; set; }

    public bool IsSearchable { get; set; }

    public DateTimeOffset? AfterTimestampFromAttribute { get; set; }

    public List<ProjectionVersionDto> Versions { get; set; }
}

public class ProjectionVersionDto
{
    public string Hash { get; set; }

    public int Revision { get; set; }

    public string Status { get; set; }

    public string RebuildStatus { get; set; }
}
