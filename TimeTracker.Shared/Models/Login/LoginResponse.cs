namespace TimeTracker.Shared.Models.Login;

public record struct LoginResponse(bool IsSuccessful, string? Error = null, string? Token = null);