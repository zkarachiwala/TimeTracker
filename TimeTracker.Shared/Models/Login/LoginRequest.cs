using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Shared.Models.Login;

public class LoginRequest
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}