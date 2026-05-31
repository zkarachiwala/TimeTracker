using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.TimeEntries;

public class TimeEntryService : ITimeEntryService
{
    private readonly IDbContextFactory<TimeTrackerDataContext> _contextFactory;
    private readonly IUserContextService _userContextService;

    public TimeEntryService(IDbContextFactory<TimeTrackerDataContext> contextFactory, IUserContextService userContextService)
    {
        _contextFactory = contextFactory;
        _userContextService = userContextService;
    }

    private async Task<string> GetUserIdAsync() =>
        await _userContextService.GetUserIdAsync() ?? throw new EntityNotFoundException("User not found.");

    private static IQueryable<TimeEntry> UserEntries(TimeTrackerDataContext ctx, string userId) =>
        ctx.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p!.ProjectDetails)
            .Where(te => te.UserId == userId && !te.Project.IsDeleted);

    private static TimeSpan CalculateTotalDuration(IEnumerable<TimeEntry> entries) =>
        entries.Where(e => e.End.HasValue)
               .Aggregate(TimeSpan.Zero, (acc, e) => acc + (e.End!.Value - e.Start));

    public async Task<TimeEntryResponse?> GetTimeEntryById(int id)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entry = await ctx.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p!.ProjectDetails)
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId);
        return entry?.Adapt<TimeEntryResponse>();
    }

    public async Task CreateTimeEntry(TimeEntryCreateRequest request)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entry = new TimeEntry
        {
            ProjectId = request.ProjectId,
            Start = request.Start,
            End = request.End,
            Note = request.Note,
            UserId = userId,
            DateCreated = DateTime.Now
        };
        ctx.TimeEntries.Add(entry);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entry = await ctx.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        entry.ProjectId = request.ProjectId;
        entry.Start = request.Start;
        entry.End = request.End;
        entry.Note = request.Note;
        entry.DateUpdated = DateTime.Now;
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteTimeEntry(int id)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entry = await ctx.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        ctx.TimeEntries.Remove(entry);
        await ctx.SaveChangesAsync();
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ToWrapper(UserEntries(ctx, userId), skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ToWrapper(UserEntries(ctx, userId).Where(te => te.ProjectId == projectId), skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ToWrapper(UserEntries(ctx, userId).Where(te => te.Start.Year == year), skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ToWrapper(UserEntries(ctx, userId).Where(te => te.Start.Year == year && te.Start.Month == month), skip, limit);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ToWrapper(UserEntries(ctx, userId)
            .Where(te => te.Start.Year == year && te.Start.Month == month && te.Start.Day == day), skip, limit);
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.Start.Year == year)
            .ToListAsync();
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<TimeEntryResponse?> GetActiveTimeEntry()
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entry = await ctx.TimeEntries
            .Include(te => te.Project)
            .ThenInclude(p => p!.ProjectDetails)
            .FirstOrDefaultAsync(te => te.UserId == userId && te.End == null && !te.Project.IsDeleted);
        return entry?.Adapt<TimeEntryResponse>();
    }

    public async Task<List<TimeEntryResponse>> GetTodaysTimeEntries()
    {
        var userId = await GetUserIdAsync();
        var today = DateTime.Today;
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.Start.Date == today)
            .OrderByDescending(te => te.Start)
            .ToListAsync();
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.ProjectId == projectId)
            .OrderByDescending(te => te.Start)
            .ToListAsync();
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    private static async Task<TimeEntryResponseWrapper> ToWrapper(IQueryable<TimeEntry> query, int skip, int limit)
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
