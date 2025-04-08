using System.Collections.Generic;

namespace One.Inception.Api.Controllers;

public partial class DomainController
{
    public class Aggregate_Response : BaseDomainModel_Response
    {
        public IEnumerable<Event_Response> Events { get; set; }

        public IEnumerable<Command_Response> Commands { get; set; }
    }

    public class AggregateIdSample_Response
    {
        public string IdSample { get; set; }
    }
}
