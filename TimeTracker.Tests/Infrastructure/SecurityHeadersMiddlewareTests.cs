using Microsoft.AspNetCore.Http;
using TimeTracker.Web.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

[Collection("Services")]
public class SecurityHeadersMiddlewareTests
{
    private static Task Next(HttpContext _) => Task.CompletedTask;

    private static async Task<IHeaderDictionary> GetResponseHeaders()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(Next);
        await middleware.InvokeAsync(context);
        return context.Response.Headers;
    }

    [Fact]
    public async Task AddsXContentTypeOptionsHeader()
    {
        var headers = await GetResponseHeaders();
        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task AddsXFrameOptionsHeader()
    {
        var headers = await GetResponseHeaders();
        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task AddsReferrerPolicyHeader()
    {
        var headers = await GetResponseHeaders();
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task AddsXXssProtectionHeader()
    {
        var headers = await GetResponseHeaders();
        Assert.Equal("0", headers["X-XSS-Protection"].ToString());
    }

    [Fact]
    public async Task AddsContentSecurityPolicyHeader()
    {
        var headers = await GetResponseHeaders();
        var csp = headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("form-action 'self' https://accounts.google.com", csp);
    }

    [Fact]
    public async Task CallsNextMiddleware()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
