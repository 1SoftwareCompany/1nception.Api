using System.Collections.Generic;

namespace One.Inception.Api.Controllers;

public partial class DomainController
{
    public class Domain_Response
    {
        public Domain_Response()
        {
            Aggregates = new List<Aggregate_Response>();
        }

        public IEnumerable<Aggregate_Response> Aggregates { get; set; }

        public IEnumerable<AggregateIdSample_Response> AggregateIdSamples { get; set; }

        public IEnumerable<Event_Response> Events { get; set; }

        public IEnumerable<Command_Response> Commands { get; set; }

        public IEnumerable<Projection_Response> Projections { get; set; }

        public IEnumerable<Saga_Response> Sagas { get; set; }

        public IEnumerable<Port_Response> Ports { get; set; }

        public IEnumerable<Gateway_Response> Gateways { get; set; }
    }
}
