using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.TimeEntries;

public class TimeEntryService : ITimeEntryService, ITimeEntryQueryService
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
            .Where(te => te.UserId == userId && !te.Project.IsDeleted);

    public async Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entry = await ctx.TimeEntries
            .Include(te => te.Project)
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId, ct);
        return entry?.Adapt<TimeEntryResponse>();
    }

    public async Task CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
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
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entry = await ctx.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId, ct)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        entry.ProjectId = request.ProjectId;
        entry.Start = request.Start;
        entry.End = request.End;
        entry.Note = request.Note;
        entry.InvoiceReference = request.InvoiceReference;
        entry.InvoicedAt = request.InvoicedAt;
        entry.DateUpdated = DateTime.Now;
        await ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteTimeEntry(int id, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entry = await ctx.TimeEntries
            .FirstOrDefaultAsync(te => te.Id == id && te.UserId == userId, ct)
            ?? throw new EntityNotFoundException($"Time entry {id} not found.");

        ctx.TimeEntries.Remove(entry);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        return await ToWrapper((ctx, uid) => UserEntries(ctx, uid), userId, skip, limit, ct);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        return await ToWrapper((ctx, uid) => UserEntries(ctx, uid).Where(te => te.ProjectId == projectId), userId, skip, limit, ct);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        return await ToWrapper((ctx, uid) => UserEntries(ctx, uid).Where(te => te.Start.Year == year), userId, skip, limit, ct);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        return await ToWrapper((ctx, uid) => UserEntries(ctx, uid).Where(te => te.Start.Year == year && te.Start.Month == month), userId, skip, limit, ct);
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        return await ToWrapper((ctx, uid) => UserEntries(ctx, uid)
            .Where(te => te.Start.Year == year && te.Start.Month == month && te.Start.Day == day), userId, skip, limit, ct);
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.Start.Year == year)
            .ToListAsync(ct);
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entry = await ctx.TimeEntries
            .Include(te => te.Project)
            .FirstOrDefaultAsync(te => te.UserId == userId && te.End == null && !te.Project.IsDeleted, ct);
        return entry?.Adapt<TimeEntryResponse>();
    }

    public async Task<List<TimeEntryResponse>> GetTodaysTimeEntries(CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        var today = DateTime.Today;
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.Start.Date == today)
            .OrderByDescending(te => te.Start)
            .ToListAsync(ct);
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    public async Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var entries = await UserEntries(ctx, userId)
            .Where(te => te.ProjectId == projectId)
            .OrderByDescending(te => te.Start)
            .ToListAsync(ct);
        return entries.Adapt<List<TimeEntryResponse>>();
    }

    private async Task<TimeEntryResponseWrapper> ToWrapper(
        Func<TimeTrackerDataContext, string, IQueryable<TimeEntry>> queryBuilder,
        string userId, int skip, int limit, CancellationToken ct)
    {
        await using var dataCtx = await _contextFactory.CreateDbContextAsync(ct);
        await using var countCtx = await _contextFactory.CreateDbContextAsync(ct);
        await using var durationCtx = await _contextFactory.CreateDbContextAsync(ct);

        var dataTask = queryBuilder(dataCtx, userId).Skip(skip).Take(Math.Min(limit, 200)).ToListAsync(ct);
        var countTask = queryBuilder(countCtx, userId).CountAsync(ct);
        var durationTask = queryBuilder(durationCtx, userId)
            .Where(te => te.End.HasValue)
            .Select(te => new { te.Start, End = te.End!.Value })
            .ToListAsync(ct);

        await Task.WhenAll(dataTask, countTask, durationTask);

        var totalDuration = durationTask.Result
            .Aggregate(TimeSpan.Zero, (acc, e) => acc + (e.End - e.Start));

        return new TimeEntryResponseWrapper
        {
            TimeEntries = dataTask.Result.Adapt<List<TimeEntryResponse>>(),
            Count = countTask.Result,
            TotalDuration = totalDuration
        };
    }
}
