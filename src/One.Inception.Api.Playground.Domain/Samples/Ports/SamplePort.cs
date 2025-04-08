using System;
using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.Ports
{
    public class SamplePort : IPort,
        IEventHandler<SampleReserved>
    {
        public SamplePort(IPublisher<ICommand> commandPublisher)
        {
            CommandPublisher = commandPublisher;
        }

        public IPublisher<ICommand> CommandPublisher { get; set; }

        public Task HandleAsync(SampleReserved @event)
        {
            Console.WriteLine($"Sample with ID: '{@event.Id}' was reserved!");

            return Task.CompletedTask;
        }
    }
}
