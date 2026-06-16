using TimeTracker.Contracts.Features.Projects;

namespace TimeTracker.Web.Features.Projects;

public class ProjectMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectResponse>()
            .Map(dest => dest.ClientName, src => src.Client != null ? src.Client.Name : null);

        config.NewConfig<Project, DeletedProjectResponse>()
            .Map(dest => dest.ClientName, src => src.Client != null ? src.Client.Name : null);
    }
}
