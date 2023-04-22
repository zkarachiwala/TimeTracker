namespace TimeTracker.API.Services;

public interface IProjectService
{
    Task<ProjectResponse?> GetProjectById(int id);
    Task<List<ProjectResponse>> GetAllProjects();
    Task<List<ProjectResponse>> CreateProject(ProjectCreateRequest request);
    Task<List<ProjectResponse>?> UpdateProject(int id, ProjectUpdateRequest request);
    Task<List<ProjectResponse>?> DeleteProject(int id);    
}