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

    private string GetUserId() =>
        _userContextService.GetUserId() ?? throw new EntityNotFoundException("User not found.");

    private static IQueryable<Project> UserProjects(TimeTrackerDataContext ctx, string userId) =>
        ctx.Projects
            .Include(p => p.ProjectDetails)
            .Where(p => !p.IsDeleted && p.ProjectUsers.Any(pu => pu.UserId == userId));

    public async Task<List<ProjectResponse>> GetAllProjects()
    {
        var userId = GetUserId();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var projects = await UserProjects(ctx, userId).ToListAsync();
        return projects.Adapt<List<ProjectResponse>>();
    }

    public async Task<ProjectResponse?> GetProjectById(int id)
    {
        var userId = GetUserId();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id);
        return project?.Adapt<ProjectResponse>();
    }

    public async Task CreateProject(ProjectCreateRequest request)
    {
        var userId = GetUserId();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var project = new Project
        {
            Name = request.Name,
            ClientId = request.ClientId,
            HourlyRate = request.HourlyRate,
            DateCreated = DateTime.Now,
            ProjectDetails = new ProjectDetails
            {
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Project = null!
            },
            ProjectUsers = new List<ProjectUser> { new() { UserId = userId } }
        };
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateProject(int id, ProjectUpdateRequest request)
    {
        var userId = GetUserId();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.Name = request.Name;
        project.ClientId = request.ClientId;
        project.HourlyRate = request.HourlyRate;
        project.DateUpdated = DateTime.Now;

        if (project.ProjectDetails is not null)
        {
            project.ProjectDetails.Description = request.Description;
            project.ProjectDetails.StartDate = request.StartDate;
            project.ProjectDetails.EndDate = request.EndDate;
        }
        else
        {
            project.ProjectDetails = new ProjectDetails
            {
                Description = request.Description,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Project = null!
            };
        }

        await ctx.SaveChangesAsync();
    }

    public async Task DeleteProject(int id)
    {
        var userId = GetUserId();
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var project = await UserProjects(ctx, userId).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.IsDeleted = true;
        project.DateDeleted = DateTime.Now;
        await ctx.SaveChangesAsync();
    }
}
