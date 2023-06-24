namespace TimeTracker.API.Services;

public interface IUserContextService
{
    string? GetUserId();

    Task<User?> GetUserAsync();
}