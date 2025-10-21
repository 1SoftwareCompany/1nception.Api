using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.Sagas
{
    public class SampleReserveSaga : Saga,
        IEventHandler<SampleCreated>
    {

        public SampleReserveSaga(IPublisher<ICommand> commandPublisher, IPublisher<IScheduledMessage> timeoutRequestPublisher)
            : base(commandPublisher, timeoutRequestPublisher)
        {
        }

        public async Task HandleAsync(SampleCreated @event)
        {
            var cmd = new ReserveSample(@event.Id);

            await commandPublisher.PublishAsync(cmd);
        }
    }
}
