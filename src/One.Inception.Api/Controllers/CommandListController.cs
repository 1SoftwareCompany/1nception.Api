using One.Inception.Discoveries;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace One.Inception.Api.Controllers;

[Route("Commands")]
public class CommandController : ApiControllerBase
{
    [HttpGet]
    public IActionResult List()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        var commandsMetaData = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(x => typeof(ICommand).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0));

        CommandListDto result = new CommandListDto();

        foreach (var cmd in commandsMetaData)
        {
            var metaToAdd = new CommandMeta();

            metaToAdd.Type = cmd.Name;
            metaToAdd.DataContract = cmd.GetContractId();

            result.Commands.Add(metaToAdd);
        }

        return new OkObjectResult(new ResponseResult<CommandListDto>(result));
    }

    public class CommandListDto
    {
        public CommandListDto()
        {
            Commands = new List<CommandMeta>();
        }

        public List<CommandMeta> Commands { get; set; }
    }

    public class CommandMeta
    {
        public string Type { get; set; }

        public string DataContract { get; set; }
    }
}
