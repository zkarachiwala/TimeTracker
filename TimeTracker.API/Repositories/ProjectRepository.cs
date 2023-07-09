using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using TimeTracker.API.Data;

namespace TimeTracker.API.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly TimeTrackerDataContext _context;
    private readonly IUserContextService _userContextService;

    public ProjectRepository(TimeTrackerDataContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }
    public async Task<List<Project>> CreateProject(Project project)
    {
        //var user = await _userContextService.GetUserAsync() ?? throw new EntityNotFoundException("User was not found.");
        var userId = _userContextService.GetUserId() ?? throw new EntityNotFoundException("User was not found.");

        // var appUser = await _context.AppUsers.FirstOrDefaultAsync(au => au.Id == userId!) ?? new AppUser { Id = userId! };
        project.ProjectUsers = new List<ProjectUser> { new ProjectUser { UserId = userId } };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return await GetAllProjects();
    }

    public async Task<List<Project>?> DeleteProject(int id)
    {
        var userId = _userContextService.GetUserId();
        if (userId is null)
            return null;

        var dbProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ProjectUsers.Any(pu => pu.UserId == userId));
        if (dbProject is null)
            return null;

        dbProject.IsDeleted = true;
        dbProject.DateDeleted = DateTime.Now;

        await _context.SaveChangesAsync();
        return await GetAllProjects();
    }

    public async Task<List<Project>> GetAllProjects()
    {
        var userId = _userContextService.GetUserId();
        if (userId is null)
            return new List<Project>();

        return await _context.Projects
            .Where(p => p.IsDeleted != true && 
                p.ProjectUsers.Any(u => u.UserId == userId))
            .ToListAsync();
    }

    public async Task<Project?> GetProjectById(int id)
    {
        var userId = _userContextService.GetUserId();
        if (userId is null)
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.ProjectUsers.Any(u => u.UserId == userId));
        return project;
    }

    public async Task<List<Project>> UpdateProject(int id, Project project)
    {
        var userId = _userContextService.GetUserId() ?? throw new EntityNotFoundException($"Entity with ID {id} was not found.");
        
        var dbProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ProjectUsers.Any(u => u.UserId == userId)) ?? 
            throw new EntityNotFoundException($"Entity with ID {id} was not found.");

        if (project.ProjectDetails is not null && dbProject.ProjectDetails is not null)
        {
            dbProject.ProjectDetails.Description = project.ProjectDetails.Description;
            dbProject.ProjectDetails.StartDate = project.ProjectDetails.StartDate;
            dbProject.ProjectDetails.EndDate = project.ProjectDetails.EndDate;
        }
        else if(project.ProjectDetails is not null && dbProject.ProjectDetails is null)
        {
            dbProject.ProjectDetails = new ProjectDetails
            {
                Project = project,
                Description = project.ProjectDetails.Description,
                StartDate = project.ProjectDetails.StartDate,
                EndDate = project.ProjectDetails.EndDate
            };            
        }

        dbProject.Name = project.Name;
        dbProject.DateUpdated = DateTime.Now;
            
        await _context.SaveChangesAsync();
        return await GetAllProjects();
    }
}