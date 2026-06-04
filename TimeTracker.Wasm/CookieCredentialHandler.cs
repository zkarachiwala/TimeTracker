using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace TimeTracker.Wasm;

public class CookieCredentialHandler : DelegatingHandler
{
    public CookieCredentialHandler() : base(new HttpClientHandler()) { }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
