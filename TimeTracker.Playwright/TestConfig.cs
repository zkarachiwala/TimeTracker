namespace TimeTracker.Playwright;

public static class TestConfig
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")
        ?? "https://localhost:7006";

    public static string AuthStatePath =>
        Path.Combine(AppContext.BaseDirectory, "playwright", ".auth", "user.json");
}
