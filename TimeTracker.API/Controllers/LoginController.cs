using Microsoft.AspNetCore.Mvc;
using TimeTracker.Shared.Models.Login;

namespace TimeTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly ILoginService _loginService;

    public LoginController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpPost]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await _loginService.Login(request);
        return Ok(result);
    }
}