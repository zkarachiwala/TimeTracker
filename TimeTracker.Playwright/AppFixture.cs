using System.Diagnostics;

namespace TimeTracker.Playwright;

public sealed class AppFixture : IAsyncLifetime
{
    private const string TestBaseUrl = "https://localhost:7007";
    private Process? _appProcess;

    public async Task InitializeAsync()
    {
        // WSL sets BROWSER=wslview which Playwright rejects — clear it so Playwright uses chromium
        Environment.SetEnvironmentVariable("BROWSER", null);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BASE_URL", TestBaseUrl);
        _appProcess = await StartAppAsync();
        await AuthenticateAsync();
    }

    public Task DisposeAsync()
    {
        if (_appProcess == null || _appProcess.HasExited)
        {
            _appProcess?.Dispose();
            return Task.CompletedTask;
        }
        try
        {
            // Cancel async reads before kill — prevents the pipe-buffer deadlock where
            // Kill(entireProcessTree:true) freezes waiting for I/O handles that never
            // receive EOF because a zombie child has them open.
            _appProcess.CancelOutputRead();
            _appProcess.CancelErrorRead();
            _appProcess.Kill(entireProcessTree: true);
            _appProcess.WaitForExit(3000);
        }
        catch { }
        finally
        {
            _appProcess.Dispose();
        }
        return Task.CompletedTask;
    }

    private static async Task<Process> StartAppAsync()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --configuration Release --launch-profile https --urls \"{TestBaseUrl}\"",
                WorkingDirectory = Path.Combine(FindRepoRoot(), "TimeTracker.Web"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        // Drain stdout/stderr asynchronously so the 64 KB pipe buffer never fills.
        // If the buffer fills the app blocks trying to write log output and stops
        // responding to HTTP requests — causing every test after the first to hang.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await WaitForAppAsync(timeoutSeconds: 180);
        return process;
    }

    private static async Task AuthenticateAsync()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        {
            // Inner scope ensures request.DisposeAsync() is called before playwright.Dispose(),
            // guaranteeing the Node.js driver does not outlive its API context.
            await using var request = await playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = TestBaseUrl,
                IgnoreHTTPSErrors = true,
            });

            var response = await request.GetAsync("/api/dev/login");
            if (!response.Ok)
                throw new Exception(
                    $"Dev login failed ({response.Status}). App must run with ASPNETCORE_ENVIRONMENT=Development.");

            Directory.CreateDirectory(Path.GetDirectoryName(TestConfig.AuthStatePath)!);
            await request.StorageStateAsync(new() { Path = TestConfig.AuthStatePath });
        }
    }

    private static async Task WaitForAppAsync(int timeoutSeconds)
    {
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        { Timeout = TimeSpan.FromSeconds(5) };

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync($"{TestBaseUrl}/login");
                if (r.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"App did not start within {timeoutSeconds}s at {TestBaseUrl}");
    }

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
