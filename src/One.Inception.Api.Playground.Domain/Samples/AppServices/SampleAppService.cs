using System.Threading.Tasks;

namespace One.Inception.Api.Playground.Domain.Samples.AppServices
{
    public class SampleAppService : ApplicationService<Sample>,
        ICommandHandler<CreateSample>,
        ICommandHandler<ReserveSample>
    {
        public SampleAppService(IAggregateRepository repository) : base(repository) { }

        public Task HandleAsync(CreateSample command)
        {
            var sample = new Sample(command.Id, command.Volume);
            return repository.SaveAsync(sample);
        }

        public Task HandleAsync(ReserveSample command)
        {
            return UpdateAsync(command.Id, ar => ar.Reserve());
        }
    }
}
