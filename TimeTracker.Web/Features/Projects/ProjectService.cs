using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Projects;

public class ProjectService : IProjectService
{
    private readonly IDbContextFactory<TimeTrackerDataContext> _contextFactory;
    private readonly IUserContextService _userContextService;

    public ProjectService(IDbContextFactory<TimeTrackerDataContext> contextFactory, IUserContextService userContextService)
    {
        _contextFactory = contextFactory;
        _userContextService = userContextService;
    }

    private async Task<string> GetUserIdAsync() =>
        await _userContextService.GetUserIdAsync() ?? throw new EntityNotFoundException("User not found.");

    private static IQueryable<Project> UserProjects(TimeTrackerDataContext ctx, string userId) =>
        ctx.Projects
            .Where(p => !p.IsDeleted && p.ProjectUsers.Any(pu => pu.UserId == userId));

    public async Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var projects = await UserProjects(ctx, userId).ToListAsync(ct);
        return projects.Adapt<List<ProjectResponse>>();
    }

    public async Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id, ct);
        return project?.Adapt<ProjectResponse>();
    }

    public async Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var project = new Project
        {
            Name = request.Name,
            ClientId = request.ClientId,
            HourlyRate = request.HourlyRate,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DateCreated = DateTime.Now,
            ProjectUsers = new List<ProjectUser> { new() { UserId = userId } }
        };
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.Name = request.Name;
        project.ClientId = request.ClientId;
        project.HourlyRate = request.HourlyRate;
        project.Description = request.Description;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.DateUpdated = DateTime.Now;

        await ctx.SaveChangesAsync(ct);
    }

    public async Task DeleteProject(int id, CancellationToken ct = default)
    {
        var userId = await GetUserIdAsync();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.IsDeleted = true;
        project.DateDeleted = DateTime.Now;
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var projects = await ctx.Projects
            .Where(p => p.IsDeleted)
            .OrderByDescending(p => p.DateDeleted)
            .ToListAsync(ct);
        return projects.Adapt<List<DeletedProjectResponse>>();
    }

    public async Task RestoreProject(int id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var project = await ctx.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, ct)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.IsDeleted = false;
        project.DateDeleted = null;
        await ctx.SaveChangesAsync(ct);
    }
}
