using System;
using System.Threading;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.Ports
{
    public class SamplePort : IPort,
        IEventHandle<SampleReserved>
    {
        public SamplePort(IPublisher<ICommand> commandPublisher)
        {
            CommandPublisher = commandPublisher;
        }

        public IPublisher<ICommand> CommandPublisher { get; set; }

        public Task HandleAsync(SampleReserved @event, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Sample with ID: '{@event.Id}' was reserved!");

            return Task.CompletedTask;
        }
    }
}
