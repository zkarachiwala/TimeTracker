using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Projects;

public class ProjectService : IProjectService
{
    private readonly TimeTrackerDataContext _context;
    private readonly IUserContextService _userContextService;

    public ProjectService(TimeTrackerDataContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    private string GetUserId() =>
        _userContextService.GetUserId() ?? throw new EntityNotFoundException("User not found.");

    private IQueryable<Project> UserProjects(string userId) =>
        _context.Projects
            .Include(p => p.ProjectDetails)
            .Where(p => !p.IsDeleted && p.ProjectUsers.Any(pu => pu.UserId == userId));

    public async Task<List<ProjectResponse>> GetAllProjects()
    {
        var userId = GetUserId();
        var projects = await UserProjects(userId).ToListAsync();
        return projects.Adapt<List<ProjectResponse>>();
    }

    public async Task<ProjectResponse?> GetProjectById(int id)
    {
        var userId = GetUserId();
        var project = await UserProjects(userId).FirstOrDefaultAsync(p => p.Id == id);
        return project?.Adapt<ProjectResponse>();
    }

    public async Task CreateProject(ProjectCreateRequest request)
    {
        var userId = GetUserId();
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
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProject(int id, ProjectUpdateRequest request)
    {
        var userId = GetUserId();
        var project = await UserProjects(userId).FirstOrDefaultAsync(p => p.Id == id)
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

        await _context.SaveChangesAsync();
    }

    public async Task DeleteProject(int id)
    {
        var userId = GetUserId();
        var project = await UserProjects(userId).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new EntityNotFoundException($"Project {id} not found.");

        project.IsDeleted = true;
        project.DateDeleted = DateTime.Now;
        await _context.SaveChangesAsync();
    }
}
