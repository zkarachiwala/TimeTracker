using System.Diagnostics;

namespace TimeTracker.Playwright;

/// <summary>
/// Runs once before all tests. Starts the app in Release mode (no PDB noise) with
/// ASPNETCORE_ENVIRONMENT=Development (so /api/dev/login is available), then obtains
/// a fresh auth session automatically. No manual steps required.
/// </summary>
[SetUpFixture]
public class GlobalSetup
{
    private Process? _appProcess;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        // WSL sets BROWSER=wslview which Playwright rejects — clear it so Playwright uses chromium
        Environment.SetEnvironmentVariable("BROWSER", null);
        _appProcess = await StartAppIfNeededAsync();
        await AuthenticateAsync();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _appProcess?.Kill(entireProcessTree: true);
        _appProcess?.Dispose();
    }

    private static async Task<Process?> StartAppIfNeededAsync()
    {
        if (await IsAppRespondingAsync())
            return null;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --configuration Release --launch-profile https",
                WorkingDirectory = Path.Combine(FindRepoRoot(), "TimeTracker.Web"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.Start();
        await WaitForAppAsync(timeoutSeconds: 180);
        return process;
    }

    private static async Task AuthenticateAsync()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var request = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = TestConfig.BaseUrl,
            IgnoreHTTPSErrors = true,
        });

        var response = await request.GetAsync("/api/dev/login");
        if (!response.Ok)
            throw new Exception(
                $"Dev login failed ({response.Status}). App must run with ASPNETCORE_ENVIRONMENT=Development.");

        Directory.CreateDirectory(Path.GetDirectoryName(TestConfig.AuthStatePath)!);
        await request.StorageStateAsync(new() { Path = TestConfig.AuthStatePath });
    }

    private static async Task<bool> IsAppRespondingAsync()
    {
        try
        {
            using var http = CreateHttpClient(timeout: 3);
            var r = await http.GetAsync($"{TestConfig.BaseUrl}/login");
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static async Task WaitForAppAsync(int timeoutSeconds)
    {
        using var http = CreateHttpClient(timeout: 5);
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync($"{TestConfig.BaseUrl}/login");
                if (r.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"App did not start within {timeoutSeconds}s at {TestConfig.BaseUrl}");
    }

    private static HttpClient CreateHttpClient(int timeout) =>
        new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        { Timeout = TimeSpan.FromSeconds(timeout) };

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "TimeTracker.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException("Could not find repo root (TimeTracker.sln)");
    }
}
