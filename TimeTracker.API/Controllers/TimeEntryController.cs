using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TimeTracker.Shared.Entities;

namespace TimeTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeEntryController : ControllerBase
{
    private static List<TimeEntry> _timeEntries = new List<TimeEntry>
    {
        new TimeEntry {
            Id = 1,
            Project = "Time Tracker App",
            End = DateTime.Now.AddHours(1)
        }
    };
    
    [HttpGet]
    public ActionResult<List<TimeEntry>> GetAllTimeEntries()
    {
        return Ok(_timeEntries);
    }

    [HttpPost]
    public ActionResult<List<TimeEntry>> CreateTimeEntry(TimeEntry timeEntry)
    {
        _timeEntries.Add(timeEntry);
        return Ok(_timeEntries);
    }

}