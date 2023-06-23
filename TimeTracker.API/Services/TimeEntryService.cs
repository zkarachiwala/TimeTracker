using TimeTracker.Shared.Models;

namespace TimeTracker.API.Services;

public class TimeEntryService : ITimeEntryService
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public TimeEntryService(ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }
    public async Task<List<TimeEntryResponse>> CreateTimeEntry(TimeEntryCreateRequest request)
    {
        var newEntry = request.Adapt<TimeEntry>();
        var result = await _timeEntryRepository.CreateTimeEntry(newEntry);
        return result.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<List<TimeEntryResponse>?> DeleteTimeEntry(int id)
    {
        var result = await _timeEntryRepository.DeleteTimeEntry(id);
        if (result is null)
            return null;
        return result.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntries()
    {
        var result = await _timeEntryRepository.GetAllTimeEntries();
        return result.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByProjectId(int projectId)
    {
        var result = await _timeEntryRepository.GetAllTimeEntriesByProjectId(projectId);
        return result.Adapt<List<TimeEntryResponse>>();        
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit)
    {
        var timeEntries = await _timeEntryRepository.GetTimeEntries(skip, limit);
        var timeEntryResponses = timeEntries.Adapt<List<TimeEntryResponse>>();
        var timeEntryCount = await _timeEntryRepository.GetTimeEntriesCount();
        return new TimeEntryResponseWrapper { TimeEntries = timeEntryResponses, Count = timeEntryCount };
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit)
    {
        var timeEntries = await _timeEntryRepository.GetTimeEntriesByProjectId(projectId, skip, limit);
        var timeEntryResponses = timeEntries.Adapt<List<TimeEntryResponse>>();
        var timeEntryCount = await _timeEntryRepository.GetTimeEntriesCountByProjectId(projectId);
        return new TimeEntryResponseWrapper { TimeEntries = timeEntryResponses, Count = timeEntryCount };
    }

    public async Task<TimeEntryResponse?> GetTimeEntryById(int id)
    {
        var result = await _timeEntryRepository.GetTimeEntryById(id);
        if (result is null)
            return null;
        return result.Adapt<TimeEntryResponse>();
    }

    public async Task<List<TimeEntryResponse>?> UpdateTimeEntry(int id, TimeEntryUpdateRequest request)
    {
        try
        {
            var updatedEntry = request.Adapt<TimeEntry>();
            var result = await _timeEntryRepository.UpdateTimeEntry(id, updatedEntry);
            return result.Adapt<List<TimeEntryResponse>>();
        }
        catch (EntityNotFoundException)
        {
            return null;
        }
    }
}

