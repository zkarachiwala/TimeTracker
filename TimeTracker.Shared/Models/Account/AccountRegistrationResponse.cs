namespace TimeTracker.Shared.Models.Account;

public record struct AccountRegistrationResponse(bool IsSuccessful, IEnumerable<string>? Errors = null);