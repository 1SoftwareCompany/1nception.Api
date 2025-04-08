using System.Collections.Generic;

namespace One.Inception.Api.Controllers;

public partial class DomainController
{
    public class Gateway_Response : BaseDomainModel_Response
    {
        public IEnumerable<Event_Response> Events { get; set; }
    }
}
