namespace TimeTracker.API.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly TimeTrackerDataContext _context;
    private readonly IUserContextService _userContextService;

    public TimeEntryRepository(TimeTrackerDataContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    public async Task<List<TimeEntry>> GetAllTimeEntries()
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return new List<TimeEntry>();

        return await _context.TimeEntries
            //.Include(te => te.Project)
            .Where(t => t.UserId == userId && !t.Project.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<TimeEntry>> CreateTimeEntry(TimeEntry timeEntry)
    {
        var userId = _userContextService.GetUserId() ?? throw new EntityNotFoundException("User was not found.");

        //var appUser = await _context.AppUsers.FirstOrDefaultAsync(au => au.Id == userId!) ?? new AppUser { Id = userId! };
        timeEntry.UserId = userId;

        _context.TimeEntries.Add(timeEntry);
        await _context.SaveChangesAsync();
        return await GetAllTimeEntries();
    }

    public async Task<List<TimeEntry>> UpdateTimeEntry(int id, TimeEntry timeEntry)
    {
        var userId = _userContextService.GetUserId() ?? throw new EntityNotFoundException("User was not found.");
        var dbTimeEntry = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId) ?? 
                throw new EntityNotFoundException($"Entity with ID {id} was not found.");
                
        dbTimeEntry.ProjectId = timeEntry.ProjectId;
        dbTimeEntry.Start = timeEntry.Start;
        dbTimeEntry.End = timeEntry.End;
        dbTimeEntry.DateUpdated = DateTime.Now;

        await _context.SaveChangesAsync();
        return await GetAllTimeEntries();
    }

    public async Task<List<TimeEntry>?> DeleteTimeEntry(int id)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return null;

        var dbTimeEntry = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId);
        if (dbTimeEntry is null)
            return null;

        _context.TimeEntries.Remove(dbTimeEntry);
        await _context.SaveChangesAsync();
        return await GetAllTimeEntries();
    }

    public async Task<TimeEntry?> GetTimeEntryById(int id)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return null;

        var timeEntry = await _context.TimeEntries
            //.Include(te => te.Project)
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId);
        return timeEntry;
    }

    public async Task<List<TimeEntry>> GetAllTimeEntriesByProjectId(int projectId)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return new List<TimeEntry>();        

        return await _context.TimeEntries
            .Where(te => te.ProjectId == projectId && te.UserId == userId)
            .ToListAsync();        
    }

    public async Task<List<TimeEntry>> GetTimeEntriesByProjectId(int projectId, int skip, int limit)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return new List<TimeEntry>();   

        return await _context.TimeEntries
            .Where(te => te.ProjectId == projectId && te.UserId == userId && !te.Project.IsDeleted)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();   
    }

    public async Task<List<TimeEntry>> GetTimeEntries(int skip, int limit)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return new List<TimeEntry>();   

        return await _context.TimeEntries
            .Where(te => te.UserId == userId && !te.Project.IsDeleted)
            .Skip(skip)
            .Take(limit)
            .ToListAsync(); 
    }

    public async Task<int> GetTimeEntriesCount()
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return 0;     
                 
        return await _context.TimeEntries
            .Where(te => te.UserId == userId && !te.Project.IsDeleted)
            .CountAsync();
    }

    public async Task<int> GetTimeEntriesCountByProjectId(int projectId)
    {
        var userId = _userContextService.GetUserId();
        if(userId is null)
            return 0;  
        
        return await _context.TimeEntries
            .Where(te => te.ProjectId == projectId && te.UserId == userId && !te.Project.IsDeleted)
            .CountAsync();
    }
}