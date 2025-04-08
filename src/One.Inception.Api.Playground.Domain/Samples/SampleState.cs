using System.Runtime.Serialization;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "20be246f-206d-4456-86a4-f4b46943f7a7")]
    public class SampleState : AggregateRootState<Sample, SampleId>
    {
        [DataMember(Order = 1)]
        public override SampleId Id { get; set; }

        [DataMember(Order = 2)]
        public bool IsReserved { get; set; }

        public bool IsNotReserved => IsReserved == false;

        public void When(SampleCreated e)
        {
            Id = e.Id;
        }

        public void When(SampleReserved e)
        {
            IsReserved = true;
        }
    }
}
