using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Tests.Infrastructure;

public sealed class FakeUserContextService(string userId) : IUserContextService
{
    public string? GetUserId() => userId;
    public Task<User?> GetUserAsync() => Task.FromResult<User?>(null);
}
