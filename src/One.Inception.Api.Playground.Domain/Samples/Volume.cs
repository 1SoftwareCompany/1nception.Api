using System;
using System.Runtime.Serialization;

namespace One.Inception.Api.Playground.Domain.Samples
{
    [DataContract(Name = "12f54e09-f844-4613-b472-6c6cf4a37c71")]
    public record class Volume
    {
        Volume() { }

        public Volume(int amount)
        {
            if (amount <= 0) throw new ArgumentException("Volume amount must be possitive.");

            Amount = amount;
        }

        [DataMember(Order = 1)]
        public int Amount { get; private set; }
    }
}
