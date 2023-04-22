namespace TimeTracker.API.Repositories;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetTimeEntryById(int id);
    Task<List<TimeEntry>> GetAllTimeEntriesByProjectId(int projectId);
    Task<List<TimeEntry>> GetAllTimeEntries();
    Task<List<TimeEntry>> CreateTimeEntry(TimeEntry timeEntry);
    Task<List<TimeEntry>> UpdateTimeEntry(int id, TimeEntry timeEntry);
    Task<List<TimeEntry>?> DeleteTimeEntry(int id);
}

