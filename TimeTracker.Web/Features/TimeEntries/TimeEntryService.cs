using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.TimeEntries;

public class TimeEntryService : ITimeEntryService
{
    private readonly TimeTrackerDataContext _context;
    private readonly IUserContextService _userContextService;

    public TimeEntryService(TimeTrackerDataContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    private string GetUserId() =>
        _userContextService.GetUserId() ?? throw new EntityNotFoundException("User not found.");

    private IQueryable<TimeEntry> UserEntries(string userId) =>
        _context.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p!.ProjectDetails)
            .Where(te => te.UserId == userId && !te.Project.IsDeleted);

    private static TimeSpan CalculateTotalDuration(IEnumerable<TimeEntry> entries) =>
        entries.Where(e => e.End.HasValue)
               .Aggregate(TimeSpan.Zero, (acc, e) => acc + (e.End!.Value - e.Start));

    public async Task<TimeEntryResponse?> GetTimeEntryById(int id)
    {
        var userId = GetUserId();
        var entry = await _context.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p!.ProjectDetails)
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId);
        return entry?.Adapt<TimeEntryResponse>();
    }

    public async Task CreateTimeEntry(TimeEntryCreateRequest request)
    {
        var userId = GetUserId();
        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            Start = request.Start,
            End = request.End,
            UserId = userId,
            DateCreated = DateTime.Now
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request)
    {
        var userId = GetUserId();
        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        entry.ProjectId = request.ProjectId;
        entry.Start = request.Start;
        entry.End = request.End;
        entry.DateUpdated = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTimeEntry(int id)
    {
        var userId = GetUserId();
        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        _context.TimeEntries.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit)
    {
        var userId = GetUserId();
        var query = UserEntries(userId);
        return await ToWrapper(query, skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit)
    {
        var userId = GetUserId();
        var query = UserEntries(userId).Where(te => te.ProjectId == projectId);
        return await ToWrapper(query, skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit)
    {
        var userId = GetUserId();
        var query = UserEntries(userId).Where(te => te.Start.Year == year);
        return await ToWrapper(query, skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit)
    {
        var userId = GetUserId();
        var query = UserEntries(userId).Where(te => te.Start.Year == year && te.Start.Month == month);
        return await ToWrapper(query, skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit)
    {
        var userId = GetUserId();
        var query = UserEntries(userId)
            .Where(te => te.Start.Year == year && te.Start.Month == month && te.Start.Day == day);
        return await ToWrapper(query, skip, limit);
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year)
    {
        var userId = GetUserId();
        var entries = await UserEntries(userId)
            .Where(te => te.Start.Year == year)
            .ToListAsync();
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    private async Task<TimeEntryResponseWrapper> ToWrapper(IQueryable<TimeEntry> query, int skip, int limit)
    {
        var all = await query.ToListAsync();
        var paged = all.Skip(skip).Take(limit).Adapt<List<TimeEntryResponse>>();
        return new TimeEntryResponseWrapper
        {
            TimeEntries = paged,
            Count = all.Count,
            TotalDuration = CalculateTotalDuration(all)
        };
    }
}
