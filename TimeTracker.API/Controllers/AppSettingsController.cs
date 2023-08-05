using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTracker.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("clientconfiguration.json")]
public class AppSettingsController : ControllerBase
{
    private readonly IClientConfigurationManager _manager;

    public AppSettingsController(IClientConfigurationManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public IActionResult GetConfiguration()
    {
        var config = _manager.GetClientConfiguration();
        return Ok(config);
    }
}