using Elders.Cronus.Api.Playground.Samples.Commands;

namespace Elders.Cronus.Api.Playground.Samples
{
    public class SampleAppService : ApplicationService<Sample>,
        ICommandHandler<CreateSample>
    {
        public SampleAppService(IAggregateRepository repository) : base(repository) { }

        public void Handle(CreateSample command)
        {
            var experience = new Sample(command.Id);
            repository.Save(experience);
        }
    }
}
