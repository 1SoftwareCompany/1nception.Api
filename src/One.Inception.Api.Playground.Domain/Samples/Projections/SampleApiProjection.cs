using One.Inception.Projections;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples
{
    /// <summary>
    /// A Projection which serves the SampleApi specific data requirements.
    /// </summary>
    /// <remarks>
    /// By applyingthe <see cref="DataContractAttribute"/> to the <see cref="SampleApiProjection"/> class
    /// you can rename the class at any point in time even after a production release.
    /// </remarks>
    [DataContract(Name = "f5bccb43-99be-4639-a9b7-1cb46e80dc7a")]
    public class SampleApiProjection : ProjectionDefinition<SampleApiProjectionState, SampleId>,
        IEventHandler<SampleCreated>,
        IEventHandler<SampleReserved>
    {
        //  * AVOID BIG collections with Cronus projections. Use ElasticSearch for search purposes.
        // List<string> collection = new List<string>();

        // * NEVER query other projections. EVER!
        // public SampleApiProjection(IProjectionReader projectionReader) { }

        public SampleApiProjection()
        {
            Subscribe<SampleCreated>(x => x.Id);
            Subscribe<SampleReserved>(x => x.Id);
        }

        public Task HandleAsync(SampleCreated @event)
        {
            State.Id = @event.Id;
            return Task.CompletedTask;
        }

        public Task HandleAsync(SampleReserved @event)
        {
            State.Id = @event.Id;
            State.IsReserved = true;
            return Task.CompletedTask;
        }
    }


    [DataContract(Name = "f197a9f7-e2cd-4d53-9ac0-97e582cd37e4")]
    public class SampleApiProjectionState
    {
        [DataMember(Order = 1)]
        public SampleId Id { get; set; }

        [DataMember(Order = 2)]
        public bool IsReserved { get; set; }
    }
}
