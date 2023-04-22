namespace TimeTracker.API.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }
    public async Task<List<ProjectResponse>> CreateProject(ProjectCreateRequest request)
    {
        var newProject = request.Adapt<Project>();
        var newProjectDetails = request.Adapt<ProjectDetails>();
        newProject.ProjectDetails = newProjectDetails;
        var result = await _projectRepository.CreateProject(newProject);
        return result.Adapt<List<ProjectResponse>>();        
    }

    public async Task<List<ProjectResponse>?> DeleteProject(int id)
    {
        var result = await _projectRepository.DeleteProject(id);
        if (result is null)
            return null;
        return result.Adapt<List<ProjectResponse>>();
    }

    public async Task<List<ProjectResponse>> GetAllProjects()
    {
        var result = await _projectRepository.GetAllProjects();
        return result.Adapt<List<ProjectResponse>>();
    }

    public async Task<ProjectResponse?> GetProjectById(int id)
    {
        var result = await _projectRepository.GetProjectById(id);
        if (result is null)
            return null;
        return result.Adapt<ProjectResponse>();        
    }

    public async Task<List<ProjectResponse>?> UpdateProject(int id, ProjectUpdateRequest request)
    {
        try
        {
            var updatedProject = request.Adapt<Project>();
            updatedProject.ProjectDetails = request.Adapt<ProjectDetails>();
            var result = await _projectRepository.UpdateProject(id, updatedProject);
            return result.Adapt<List<ProjectResponse>>();
        }
        catch (EntityNotFoundException)
        {
            return null;
        }
    }
}