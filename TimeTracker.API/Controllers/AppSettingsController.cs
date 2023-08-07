using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TimeTracker.API.Controllers;

// Pull config data from server secrets to web assembly client so ids are not hard-coded in appsettings.json on client
// Although these are not secrets, to make this available via github these are best left out of package

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