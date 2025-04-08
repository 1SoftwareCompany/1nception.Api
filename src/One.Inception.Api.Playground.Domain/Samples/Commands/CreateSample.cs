using System;
using System.Runtime.Serialization;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "f2e74744-4bf5-4a21-bdd8-ab3ad5f4d951")]
    public class CreateSample : ICommand
    {
        CreateSample()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public CreateSample(SampleId id, Volume volume) : this()
        {
            Id = id;
            Volume = volume;
        }

        [DataMember(Order = 1)]
        public SampleId Id { get; private set; }

        [DataMember(Order = 2)]
        public Volume Volume { get; set; }

        public DateTimeOffset Timestamp { get; private set; }
    }
}
