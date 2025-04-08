using One.Inception.Projections;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "2cb8a10c-1ca0-41cd-a451-53dc520a4085")]
    public class IsSampleReservedProjection : ProjectionDefinition<SampleApiProjectionState, SampleId>,
        IEventHandler<SampleReserved>
    {
        public IsSampleReservedProjection()
        {
            Subscribe<SampleReserved>(x => x.Id);
        }

        public Task HandleAsync(SampleReserved @event)
        {
            State.IsReserved = true;
            return Task.CompletedTask;
        }

        [DataContract(Name = "0d4d9eda-6489-4cee-9303-0e931bb146d4")]
        public class IsSampleReservedState
        {
            [DataMember(Order = 1)]
            public bool IsReserved { get; set; }
        }
    }
}
