using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Shared.Entities;

public class UserId
{
    [Key]
    public required string Id { get; set; }
}