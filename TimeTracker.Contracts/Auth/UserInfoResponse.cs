namespace TimeTracker.Contracts.Auth;

public record UserInfoResponse(bool IsAuthenticated, string? Email, string[] Roles);
