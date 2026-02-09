using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.Gateways
{
    public class SampleGateway : IGateway,
        IEventHandle<SampleReserved>
    {
        public Task HandleAsync(SampleReserved @event)
        {
            Console.WriteLine($"Sample with ID: '{@event.Id}' was reserved!");
            return Task.CompletedTask;
        }
    }
}
