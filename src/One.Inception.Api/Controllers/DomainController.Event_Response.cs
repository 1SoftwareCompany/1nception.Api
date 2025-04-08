using System.Collections.Generic;

namespace One.Inception.Api.Controllers;

public partial class DomainController
{
    public class Event_Response : BaseSerializableDomainModel_Response
    {
        public Event_Response()
        {
            Properties = new List<string>();
        }

        public List<string> Properties { get; set; }

        public bool IsPublicEvent { get; set; }
    }
}
