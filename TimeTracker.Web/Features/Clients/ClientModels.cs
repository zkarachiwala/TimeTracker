using TimeTracker.Contracts.Features.Clients;
namespace TimeTracker.Web.Features.Clients;

public class ClientMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TimeTracker.Shared.Entities.Client, ClientResponse>();
        config.NewConfig<TimeTracker.Shared.Entities.Client, DeletedClientResponse>();
    }
}
