using TimeTracker.Shared.Entities;

namespace TimeTracker.API.Shared;

public interface IUserContextService
{
    string? GetUserId();
    Task<User?> GetUserAsync();
}
