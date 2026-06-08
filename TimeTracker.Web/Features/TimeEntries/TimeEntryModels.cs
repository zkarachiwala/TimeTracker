using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Web.Features.TimeEntries;

public class TimeEntryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TimeEntry, TimeEntryResponse>()
            .Map(dest => dest.Project, src => new ProjectSummary(
                src.Project != null ? src.Project.Id : 0,
                src.Project != null ? src.Project.Name : string.Empty));
    }
}
