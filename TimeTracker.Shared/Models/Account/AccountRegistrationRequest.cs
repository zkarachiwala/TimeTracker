namespace TimeTracker.Shared.Models.Account;

public class AccountRegistrationRequest
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
}