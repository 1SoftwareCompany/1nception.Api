using System;
using System.Runtime.Serialization;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "8d710982-9126-44a5-acce-e0db20d93a43")]
    public class ReserveSample : ICommand
    {
        ReserveSample()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public ReserveSample(SampleId id) : this()
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        [DataMember(Order = 1)]
        public SampleId Id { get; private set; }

        [DataMember(Order = 1)]
        public DateTimeOffset Timestamp { get; private set; }
    }
}
