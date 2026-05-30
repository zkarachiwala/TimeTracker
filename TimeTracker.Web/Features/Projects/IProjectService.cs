namespace TimeTracker.Web.Features.Projects;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetAllProjects();
    Task<ProjectResponse?> GetProjectById(int id);
    Task CreateProject(ProjectCreateRequest request);
    Task UpdateProject(int id, ProjectUpdateRequest request);
    Task DeleteProject(int id);
}
