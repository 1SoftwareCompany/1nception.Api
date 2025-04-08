using System.Runtime.Serialization;

namespace Elders.Cronus.Api.Playground.Samples.Commands
{
    [DataContract(Name = "e928ffab-1bc6-40e2-af5f-c503d9ec7455")]
    public class CreateSample : ICommand
    {
        CreateSample() { }

        public CreateSample(SampleId id)
        {
            Id = id;
        }

        [DataMember(Order = 1)]
        public SampleId Id { get; private set; }
    }
}
