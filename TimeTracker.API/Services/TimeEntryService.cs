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

    public async Task<List<TimeEntryByProjectResponse>> GetAllTimeEntriesByProjectId(int projectId)
    {
        var result = await _timeEntryRepository.GetAllTimeEntriesByProjectId(projectId);
        return result.Adapt<List<TimeEntryByProjectResponse>>();        
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

