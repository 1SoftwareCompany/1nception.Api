using System.Runtime.Serialization;

namespace Elders.Cronus.Api.Playground.Samples.Events
{
    [DataContract(Name = "f6ce6108-ed2e-4367-8711-c48da619b6df")]
    public class SampleCreated : IEvent
    {
        SampleCreated() { }

        public SampleCreated(SampleId id)
        {
            Id = id;
        }

        [DataMember(Order = 1)]
        public SampleId Id { get; private set; }
    }
}
