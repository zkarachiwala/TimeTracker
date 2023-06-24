using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Shared.Models;

namespace TimeTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TimeEntryController : ControllerBase
{
    private readonly ITimeEntryService _timeEntryService;

    public TimeEntryController(ITimeEntryService timeEntryService)
    {
        _timeEntryService = timeEntryService;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<TimeEntryResponse>>> GetAllTimeEntries()
    {
        return Ok(await _timeEntryService.GetAllTimeEntries());
    }

    [HttpGet("{skip}/{limit}")]
    public async Task<ActionResult<TimeEntryResponseWrapper>> GetTimeEntries(int skip, int limit)
    {
        return Ok(await _timeEntryService.GetTimeEntries(skip, limit));
    }

    [HttpGet("project/{projectId}/{skip}/{limit}")]
    public async Task<ActionResult<TimeEntryResponseWrapper>> GetTimeEntriesByProjectId(int projectId, int skip, int limit)
    {
        return Ok(await _timeEntryService.GetTimeEntriesByProjectId(projectId, skip, limit));
    }    

    [HttpGet("{id}")]
    public async Task<ActionResult<TimeEntryResponse>> GetTimeEntryById(int id)
    {
        var result = await  _timeEntryService.GetTimeEntryById(id);
        if(result is null)
            return NotFound("TimeEntry with the given ID was not found.");

        return Ok(result);
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<TimeEntryResponse>> GetAllTimeEntriesByProjectId(int projectId)
    {
        return Ok(await _timeEntryService.GetAllTimeEntriesByProjectId(projectId));
    }

    [HttpPost]
    public async Task<ActionResult<List<TimeEntryResponse>>> CreateTimeEntry(TimeEntryCreateRequest timeEntry)
    {
        return Ok(await _timeEntryService.CreateTimeEntry(timeEntry));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<List<TimeEntryResponse>>> UpdateTimeEntry(int id, TimeEntryUpdateRequest timeEntry)
    {
        var result = await _timeEntryService.UpdateTimeEntry(id, timeEntry);
        if (result is null)
            return NotFound("TimeEntry with the given ID was not found.");

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<List<TimeEntryResponse>>> DeleteTimeEntry(int id)
    {
        var result = await _timeEntryService.DeleteTimeEntry(id);
        if (result is null)
            return NotFound("TimeEntry with the given ID was not found.");

        return Ok(result);
    }
}