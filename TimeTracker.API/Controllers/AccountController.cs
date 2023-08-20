using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Shared.Models.Account;
using System.Security.Claims;

namespace TimeTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly UserManager<User> _userManager;
    public AccountController(IAccountService accountService, UserManager<User> userManager)
    {
        _userManager = userManager;
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<ActionResult<AccountRegistrationResponse>> Register(AccountRegistrationRequest request)
    {
        var result = await _accountService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("role")]
    public async Task<IActionResult> AssignRole(string userName, string roleName)
    {
        await _accountService.AssignRole(userName, roleName);
        return Ok();
    }

    [HttpGet("role")]
    public async Task<ActionResult<List<string>>> GetRoles()
    {
        var roles = new List<string>();
        var user = await _userManager.FindByNameAsync(User.Identity.Name);
        if (user is not null)
            roles = await _accountService.GetRolesAsync(user);
        return Ok(roles);

    }
}