using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;

namespace TimeTracker.Playwright;

public sealed class ShowcaseFixture : IAsyncLifetime
{
    public const string ShowcaseBaseUrl = "http://localhost:7008";
    private string? _publishDir;
    private WebApplication? _host;

    public async Task InitializeAsync()
    {
        // WSL sets BROWSER=wslview which Playwright rejects — clear it so Playwright uses chromium
        Environment.SetEnvironmentVariable("BROWSER", null);
        _publishDir = Path.Combine(Path.GetTempPath(), $"tt-showcase-{Guid.NewGuid():N}");
        var repoRoot = FindRepoRoot();
        await PublishAsync(repoRoot);
        await StartServerAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            await _host.DisposeAsync();
        }
        if (_publishDir != null)
        {
            try { Directory.Delete(_publishDir, recursive: true); } catch { }
        }
    }

    private async Task PublishAsync(string repoRoot)
    {
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish TimeTracker.Client/TimeTracker.Client.csproj -p:Showcase=true -p:DefineConstants=SHOWCASE -c Release --output \"{_publishDir}\" --nologo -v q",
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        // Drain pipes asynchronously to avoid pipe buffer deadlock
        proc.OutputDataReceived += (_, _) => { };
        proc.ErrorDataReceived += (_, _) => { };
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
            throw new Exception($"Showcase publish failed (exit code {proc.ExitCode})");
    }

    private async Task StartServerAsync()
    {
        var wwwroot = Path.Combine(_publishDir!, "wwwroot");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            WebRootPath = wwwroot,
            Args = ["--urls", ShowcaseBaseUrl],
        });
        builder.Logging.ClearProviders();
        _host = builder.Build();
        _host.UsePathBase("/TimeTracker");

        // ServeUnknownFileTypes is required for Blazor WASM framework assets.
        // UseStaticFiles returns 404 for any extension not in FileExtensionContentTypeProvider.
        // Blazor publishes .dat (ICU data), .wasm, .blat, .webcil etc. — .dat has no
        // registered MIME type by default, so without this flag the WASM runtime 404s on
        // icudt_EFIGS.dat and crashes before the app can render.
        _host.UseStaticFiles(new StaticFileOptions
        {
            ServeUnknownFileTypes = true,
            DefaultContentType = "application/octet-stream",
        });
        _host.MapFallbackToFile("index.html");
        await _host.StartAsync();
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
