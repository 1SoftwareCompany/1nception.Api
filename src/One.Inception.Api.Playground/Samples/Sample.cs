using Elders.Cronus.Api.Playground.Samples.Events;

namespace Elders.Cronus.Api.Playground.Samples
{
    public class Sample : AggregateRoot<SampleState>
    {
        private Sample() { }

        public Sample(SampleId sampleId)
        {
            var @event = new SampleCreated(sampleId);
            Apply(@event);
        }
    }

    public class Sample : AggregateRoot<SampleState>
    {
        private Sample() { }

        public Sample(SampleId sampleId)
        {
            var @event = new SampleCreated(sampleId);
            Apply(@event);
        }
    }
}
