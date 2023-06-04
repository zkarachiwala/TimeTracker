using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Shared.Models.TimeEntry;

public class TimeEntryRequest
{
    [Range(1,int.MaxValue, ErrorMessage = "Please select a project.")]
    public int ProjectId { get; set; }
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; }
}