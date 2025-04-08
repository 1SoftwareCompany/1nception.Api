using System;

namespace One.Inception.Api.Playground.Domain.Samples
{
    public class Sample : AggregateRoot<SampleState>
    {
        Sample() { }

        public Sample(SampleId experienceId, Volume volume)
        {
            Apply(new SampleCreated(experienceId, volume, DateTimeOffset.UtcNow));
        }

        public void Reserve()
        {
            if (state.IsNotReserved)
            {
                var @event = new SampleReserved(state.Id, DateTimeOffset.UtcNow);
                Apply(@event);
            }
        }
    }
}
