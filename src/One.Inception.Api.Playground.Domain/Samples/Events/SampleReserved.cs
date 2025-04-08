using System;
using System.Runtime.Serialization;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "c44ad7ac-a64c-4751-973d-735db52fa9d5")]
    public class SampleReserved : IEvent
    {
        SampleReserved() { }

        public SampleReserved(SampleId id, DateTimeOffset timestamp)
        {
            Id = id;
            Timestamp = timestamp;
        }

        [DataMember(Order = 1)]
        public SampleId Id { get; set; }

        [DataMember(Order = 2)]
        public DateTimeOffset Timestamp { get; private set; }
    }
}
