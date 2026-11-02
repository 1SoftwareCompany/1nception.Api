using System;
using System.Threading;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.Gateways
{
    public class SampleGateway : IGateway,
        IEventHandle<SampleReserved>
    {
        public Task HandleAsync(SampleReserved @event, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Sample with ID: '{@event.Id}' was reserved!");
            return Task.CompletedTask;
        }
    }
}
