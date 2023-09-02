using Microsoft.EntityFrameworkCore.Internal;

namespace TimeTracker.API.Repositories;

public interface ITimeEntryRepository
{
    Task<TimeEntry?> GetTimeEntryById(int id);
    Task<List<TimeEntry>> GetAllTimeEntriesByProjectId(int projectId);
    Task<List<TimeEntry>> GetAllTimeEntries();
    Task<List<TimeEntry>> CreateTimeEntry(TimeEntry timeEntry);
    Task<List<TimeEntry>> UpdateTimeEntry(int id, TimeEntry timeEntry);
    Task<List<TimeEntry>?> DeleteTimeEntry(int id);
    Task<List<TimeEntry>> GetTimeEntriesByProjectId(int projectId, int skip, int limit);
    Task<List<TimeEntry>> GetTimeEntries(int skip, int limit);
    Task<int> GetTimeEntriesCount();
    Task<int> GetTimeEntriesCountByProjectId(int projectId);
    Task<int> GetTimeEntriesCountByYear(int year);
    Task<int> GetTimeEntriesCountByMonth(int month, int year);
    Task<int> GetTimeEntriesCountByDay(int day, int month, int year);
    Task<List<TimeEntry>> GetTimeEntriesByYear(int year, int skip, int limit);
    Task<List<TimeEntry>> GetTimeEntriesByMonth(int month, int year, int skip, int limit);
    Task<List<TimeEntry>> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit);
}

