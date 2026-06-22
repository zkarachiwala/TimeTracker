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

        // Release publish only includes compressed variants (.br, .gz) of large framework
        // assets like icudt.dat — the uncompressed file doesn't exist in the output.
        // This middleware serves the compressed variant transparently so the WASM runtime
        // can load ICU data without 404s. Mirrors what UseBlazorFrameworkFiles() does in
        // a hosted Blazor project.
        var contentTypes = new FileExtensionContentTypeProvider();
        _host.Use(async (ctx, next) =>
        {
            var requestPath = ctx.Request.Path.Value ?? "";
            var physicalPath = Path.Combine(wwwroot, requestPath.TrimStart('/'));
            if (!File.Exists(physicalPath))
            {
                var accept = ctx.Request.Headers.AcceptEncoding.ToString();
                var (compressed, encoding) =
                    accept.Contains("br") && File.Exists(physicalPath + ".br")
                        ? (physicalPath + ".br", "br")
                        : accept.Contains("gzip") && File.Exists(physicalPath + ".gz")
                            ? (physicalPath + ".gz", "gzip")
                            : (null, null);
                if (compressed != null)
                {
                    ctx.Response.Headers.ContentEncoding = encoding;
                    if (contentTypes.TryGetContentType(physicalPath, out var ct))
                        ctx.Response.ContentType = ct;
                    ctx.Response.ContentLength = new FileInfo(compressed).Length;
                    await using var fs = File.OpenRead(compressed);
                    await fs.CopyToAsync(ctx.Response.Body);
                    return;
                }
            }
            await next(ctx);
        });

        _host.UseStaticFiles();
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
