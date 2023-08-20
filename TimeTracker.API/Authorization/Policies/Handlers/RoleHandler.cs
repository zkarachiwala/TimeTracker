// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-7.0
using System.Security.Claims;
using TimeTracker.API.Authorization.Policies.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

public class RoleHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IAccountService _accountService;
    private readonly UserManager<User> _userManager;
    public RoleHandler(IAccountService accountService, UserManager<User> userManager)
    {
        _userManager = userManager;
        _accountService = accountService;
        
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement roleRequirement)
    {
        var roles = new List<string>();
        var nameId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByNameAsync(nameId);
        
        if (user is not null)        
        {
            roles = await _accountService.GetRolesAsync(user);
            if (roles.FirstOrDefault(role => role == roleRequirement.RoleName) != null)
            {
                Console.WriteLine("Is Admin");
                context.Succeed(roleRequirement);
            }
        }
    }
}