using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Shared;

public interface IUserContextService
{
    string? GetUserId();
    Task<User?> GetUserAsync();
}
