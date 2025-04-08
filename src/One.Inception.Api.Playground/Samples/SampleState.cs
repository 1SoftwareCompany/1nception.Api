using Elders.Cronus.Api.Playground.Samples.Events;
using System.Runtime.Serialization;

namespace Elders.Cronus.Api.Playground.Samples
{
    public class SampleState : AggregateRootState<Sample, SampleId>
    {
        [DataMember(Order = 1)]
        public override SampleId Id { get; set; }


        public void When(SampleCreated e)
        {
            Id = e.Id;
        }
    }
}
