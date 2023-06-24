using Microsoft.AspNetCore.Mvc;
using TimeTracker.Shared.Models.Account;

namespace TimeTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<ActionResult<AccountRegistrationResponse>> Register(AccountRegistrationRequest request)
    {
        var result = await _accountService.RegisterAsync(request);
        return Ok(result);
    }
}