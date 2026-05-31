# Testing Errors

## Login Page
- No logo
- Width of dialog is signigicantly wider than the design

## Home Page
- No logo
- Header bar / text are not the same as mockup
- Spacing of center content is not same as mockup
- Logout is not in the bottom left corner as per design, but in the top right corner as a link with the username. This is a common pattern for web apps, but it is not what was designed.
- The '+' button is not rounded like mockup.  I am happy to adopt modern design but we need to be consistent across the app.
- Hamburger menu does not work
- My name instead of initials is appearing in the top right corner. This is a common pattern for web apps, but it is not what was designed.
- No search icon but we havent specified this yet so that is ok.
- Error on load
```
Unhandled exception rendering component: Cannot access a disposed context instance. A common cause of this error is disposing a context instance that was resolved from dependency injection and then later trying to use the same context instance elsewhere in your application. This may occur if you are calling 'Dispose' on the context instance, or wrapping it in a using statement. If you are using dependency injection, you should let the dependency injection container take care of disposing context instances.
      Object name: 'TimeTrackerDataContext'.
      System.ObjectDisposedException: Cannot access a disposed context instance. A common cause of this error is disposing a context instance that was resolved from dependency injection and then later trying to use the same context instance elsewhere in your application. This may occur if you are calling 'Dispose' on the context instance, or wrapping it in a using statement. If you are using dependency injection, you should let the dependency injection container take care of disposing context instances.
      Object name: 'TimeTrackerDataContext'.
         at Microsoft.EntityFrameworkCore.DbContext.CheckDisposed()
         at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
         at Microsoft.EntityFrameworkCore.DbContext.Set[TEntity]()
         at TimeTracker.Web.Data.TimeTrackerDataContext.get_TimeEntries() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Data/TimeTrackerDataContext.cs:line 26
         at TimeTracker.Web.Features.TimeEntries.TimeEntryService.GetActiveTimeEntry() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/TimeEntries/TimeEntryService.cs:line 134
         at TimeTracker.Web.Features.Timer.Pages.TimerPage.LoadData() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/Timer/Pages/TimerPage.razor:line 174
         at TimeTracker.Web.Features.Timer.Pages.TimerPage.OnInitializedAsync() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/Timer/Pages/TimerPage.razor:line 169
         at Microsoft.AspNetCore.Components.ComponentBase.RunInitAndSetParametersAsync()
         at Microsoft.AspNetCore.Components.RenderTree.Renderer.GetErrorHandledTask(Task taskToHandle, ComponentState owningComponentState)
fail: Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost[111]
      Unhandled exception in circuit '3PBTNMnC_3u8YhI2eZ-demmQasfrXYfi1nToaNk_h3Q'.
      System.ObjectDisposedException: Cannot access a disposed context instance. A common cause of this error is disposing a context instance that was resolved from dependency injection and then later trying to use the same context instance elsewhere in your application. This may occur if you are calling 'Dispose' on the context instance, or wrapping it in a using statement. If you are using dependency injection, you should let the dependency injection container take care of disposing context instances.
      Object name: 'TimeTrackerDataContext'.
         at Microsoft.EntityFrameworkCore.DbContext.CheckDisposed()
         at Microsoft.EntityFrameworkCore.DbContext.get_DbContextDependencies()
         at Microsoft.EntityFrameworkCore.DbContext.Set[TEntity]()
         at TimeTracker.Web.Data.TimeTrackerDataContext.get_TimeEntries() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Data/TimeTrackerDataContext.cs:line 26
         at TimeTracker.Web.Features.TimeEntries.TimeEntryService.GetActiveTimeEntry() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/TimeEntries/TimeEntryService.cs:line 134
         at TimeTracker.Web.Features.Timer.Pages.TimerPage.LoadData() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/Timer/Pages/TimerPage.razor:line 174
         at TimeTracker.Web.Features.Timer.Pages.TimerPage.OnInitializedAsync() in /home/zkarachiwala/repos/TimeTracker/TimeTracker.Web/Features/Timer/Pages/TimerPage.razor:line 169
         at Microsoft.AspNetCore.Components.ComponentBase.RunInitAndSetParametersAsync()
         at Microsoft.AspNetCore.Components.RenderTree.Renderer.GetErrorHandledTask(Task taskToHandle, ComponentState owningComponentState)
```
- '+' Button doesn't do anything

## Entries Page
- Same formatting issues as home page
- Highlight of menu bar is different than mockup (blue highlight on active page in mockup and rounded, grey in app with no rounding)
- Top bar with DAY, MONTH, YEAR, PROJECT is wrong colour and not properly spaced
- Day picker left and right arrows do not do anything

## Reports Page
- Same formatting issues as home page

