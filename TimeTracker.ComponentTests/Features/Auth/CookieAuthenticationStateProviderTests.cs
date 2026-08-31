using System.Net;
using TimeTracker.Client.Features.Auth;

namespace TimeTracker.ComponentTests.Features.Auth;

/// <summary>Delegating handler returning a fixed response, or throwing to simulate a transport
/// failure (backend asleep/unreachable) rather than a definitive server answer.</summary>
internal class StubHttpMessageHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        Task.FromResult(respond());
}

public class CookieAuthenticationStateProviderTests
{
    private static CookieAuthenticationStateProvider MakeProvider(Func<HttpResponseMessage> respond)
    {
        var http = new HttpClient(new StubHttpMessageHandler(respond)) { BaseAddress = new Uri("http://localhost/") };
        return new CookieAuthenticationStateProvider(http);
    }

    [Fact]
    public async Task TransportFailure_DoesNotCacheAnonymous_SoNextCallRetries()
    {
        var callCount = 0;
        var provider = MakeProvider(() =>
        {
            callCount++;
            throw new HttpRequestException("Simulated backend unreachable");
        });

        var first = await provider.GetAuthenticationStateAsync();
        var second = await provider.GetAuthenticationStateAsync();

        Assert.False(first.User.Identity!.IsAuthenticated);
        Assert.False(second.User.Identity!.IsAuthenticated);
        Assert.Equal(2, callCount); // not cached — both calls hit the transport
    }

    [Fact]
    public async Task DefinitiveAnonymousResponse_IsCached_SoNextCallDoesNotRetry()
    {
        var callCount = 0;
        var provider = MakeProvider(() =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"isAuthenticated":false,"email":null,"roles":[]}""",
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var first = await provider.GetAuthenticationStateAsync();
        var second = await provider.GetAuthenticationStateAsync();

        Assert.False(first.User.Identity!.IsAuthenticated);
        Assert.False(second.User.Identity!.IsAuthenticated);
        Assert.Equal(1, callCount); // cached — second call reuses the definitive answer
    }

    [Fact]
    public async Task DefinitiveAuthenticatedResponse_IsCached()
    {
        var callCount = 0;
        var provider = MakeProvider(() =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"isAuthenticated":true,"email":"zak@dzk.com.au","roles":[]}""",
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var first = await provider.GetAuthenticationStateAsync();
        var second = await provider.GetAuthenticationStateAsync();

        Assert.True(first.User.Identity!.IsAuthenticated);
        Assert.True(second.User.Identity!.IsAuthenticated);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Refresh_ClearsCache_SoNextCallReflectsNewServerState()
    {
        var isAuthenticated = true;
        var provider = MakeProvider(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"isAuthenticated":{{(isAuthenticated ? "true" : "false")}},"email":"zak@dzk.com.au","roles":[]}""",
                System.Text.Encoding.UTF8, "application/json")
        });

        var before = await provider.GetAuthenticationStateAsync();
        Assert.True(before.User.Identity!.IsAuthenticated);

        // Server state changes; Refresh() must drop the stale cached answer synchronously
        // (before any background re-fetch completes) so the next call can't see it.
        isAuthenticated = false;
        provider.Refresh();

        var after = await provider.GetAuthenticationStateAsync();
        Assert.False(after.User.Identity!.IsAuthenticated);
    }
}
