using Microsoft.JSInterop;

namespace TimeTracker.Client.Shared;

public interface ILocalStore
{
    ValueTask<string?> GetItemAsync(string key);
    ValueTask SetItemAsync(string key, string value);
    ValueTask RemoveItemAsync(string key);
}

public class LocalStore(IJSRuntime js) : ILocalStore
{
    public ValueTask<string?> GetItemAsync(string key) =>
        js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetItemAsync(string key, string value) =>
        js.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveItemAsync(string key) =>
        js.InvokeVoidAsync("localStorage.removeItem", key);
}
