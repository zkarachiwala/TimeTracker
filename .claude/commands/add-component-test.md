# /add-component-test

Add a bUnit component test for a Blazor component in this project.

## Usage

`/add-component-test <ComponentName>`

Example: `/add-component-test EntryRow`

## Test class pattern

If the component uses any MudBlazor components, extend `MudBlazorContext`. Otherwise extend `BunitContext` directly.

```csharp
using Bunit;
using TimeTracker.ComponentTests.Fixtures;

namespace TimeTracker.ComponentTests.Features.<FeatureName>;

public class <ComponentName>Tests : MudBlazorContext
{
    [Fact]
    public void Renders_WhenGivenValidProps()
    {
        var cut = Render<<ComponentName>>(p => p
            .Add(e => e.SomeProp, someValue));

        Assert.Contains("expected text", cut.Markup);
    }
}
```

## MudBlazorContext — when and why

`MudBlazorContext` (in `TimeTracker.ComponentTests/Fixtures/MudBlazorContext.cs`) extends `BunitContext` and:
- Registers MudBlazor services with zero transition durations (avoids fake-timer issues in snapshot tests)
- Sets `JSInterop.Mode = JSRuntimeMode.Loose` to stub MudBlazor JS interop
- Pre-renders `MudPopoverProvider` (required for `MudTooltip`, `MudMenu`, and other popover-based components)
- Implements `IAsyncLifetime` so xUnit uses `DisposeAsync()` — required because MudBlazor services are `IAsyncDisposable`-only and throw if synchronously disposed

**Always use `IAsyncLifetime` on any context that holds `IAsyncDisposable` services.** Do not add a sync `Dispose()` override.

## Rendering

Use `Render<T>()` (bUnit 2.x). Never use the old `RenderComponent<T>()` form.

## Mocking services

Inject fakes via `Services.AddSingleton<IMyService>(new FakeMyService())` in the test constructor before calling `Render<T>()`. For one-off stubs, use `Substitute.For<IMyService>()` from NSubstitute.

## Where tests live

`TimeTracker.ComponentTests/Features/<FeatureName>/<ComponentName>Tests.cs`

## Run command

```bash
dotnet test TimeTracker.ComponentTests
```

No running database or browser required.
