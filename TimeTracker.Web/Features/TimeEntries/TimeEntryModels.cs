using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Web.Features.TimeEntries;

public class TimeEntryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // SQL Server datetime2 has no timezone info — EF Core returns Kind=Unspecified.
        // Explicitly mark as Utc so System.Text.Json serializes with a 'Z' suffix,
        // letting WASM clients call .ToLocalTime() to get the correct browser-local time.
        config.NewConfig<TimeEntry, TimeEntryResponse>()
            .Map(dest => dest.Start, src => DateTime.SpecifyKind(src.Start, DateTimeKind.Utc))
            .Map(dest => dest.End, src => src.End.HasValue
                ? (DateTime?)DateTime.SpecifyKind(src.End.Value, DateTimeKind.Utc)
                : null)
            .Map(dest => dest.Project, src => new ProjectSummary(
                src.Project != null ? src.Project.Id : 0,
                src.Project != null ? src.Project.Name : string.Empty));

        config.NewConfig<TimeEntry, DeletedTimeEntryResponse>()
            .Map(dest => dest.Start, src => DateTime.SpecifyKind(src.Start, DateTimeKind.Utc))
            .Map(dest => dest.End, src => src.End.HasValue
                ? (DateTime?)DateTime.SpecifyKind(src.End.Value, DateTimeKind.Utc)
                : null)
            .Map(dest => dest.ProjectName, src => src.Project != null ? src.Project.Name : string.Empty);
    }
}
