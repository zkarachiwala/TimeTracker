using Microsoft.AspNetCore.Authorization;

namespace TimeTracker.API.Authorization.Policies.Requirements;

public class RoleRequirement : IAuthorizationRequirement
{
    public RoleRequirement(string roleName) => RoleName = roleName;

    public string RoleName { get; }
}