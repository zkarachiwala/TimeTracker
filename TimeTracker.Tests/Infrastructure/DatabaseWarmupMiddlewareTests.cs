using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using TimeTracker.Web.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

[Collection("Services")]
public class DatabaseWarmupMiddlewareTests
{
    private static DefaultHttpContext CreateContext(string? path = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        if (path is not null)
            ctx.Request.Path = path;
        return ctx;
    }

    [Fact]
    public async Task PassesThroughWhenNextSucceeds()
    {
        var nextCalled = false;
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Returns503HtmlWhenSqlExceptionIsConnectivityError()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: 4060));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("text/html", context.Response.ContentType);
        Assert.Contains("Waking up", await ReadResponseBody(context));
    }

    [Fact]
    public async Task Returns503ForTimeoutSqlException()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: -2));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("text/html", context.Response.ContentType);
    }

    [Fact]
    public async Task Returns503ForDbException()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw new TestDbException());

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("text/html", context.Response.ContentType);
    }

    [Fact]
    public async Task Returns503ForInvalidOperationExceptionWithConnectionMessage()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw new InvalidOperationException(
                "An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency." +
                "No connection could be made because the target machine actively refused it."));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
    }

    [Fact]
    public async Task RethrowsInvalidOperationExceptionWithoutConnectionMessage()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw new InvalidOperationException("Some other error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task ReturnsProblemJsonForApiPath()
    {
        var context = CreateContext(path: "/api/timeentries/active");
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: -2));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("application/problem+json", context.Response.ContentType);
        var body = await ReadResponseBody(context);
        Assert.Contains("503", body);
        Assert.Contains("waking up", body);
    }

    [Fact]
    public async Task RethrowsWhenResponseHasAlreadyStarted()
    {
        var context = CreateContext();
        context.Features.Set<IHttpResponseFeature>(new SettableResponseFeature { HasStarted = true });

        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: 53));

        await Assert.ThrowsAsync<SqlException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task RethrowsSqlExceptionWithNonConnectivityNumber()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: 515)); // Cannot insert NULL

        await Assert.ThrowsAsync<SqlException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task Returns503ForWrappedSqlException()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw new InvalidOperationException("outer",
                CreateSqlException(number: 10054)));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
        Assert.Contains("text/html", context.Response.ContentType);
    }

    [Fact]
    public async Task Returns503ForTransportLevelError()
    {
        var context = CreateContext();
        var middleware = new DatabaseWarmupMiddleware(_ =>
            throw CreateSqlException(number: 10054));

        await middleware.InvokeAsync(context);

        Assert.Equal(503, context.Response.StatusCode);
    }

    private static SqlException CreateSqlException(int number, string message = "A network-related error occurred")
    {
        var errorCtor = typeof(SqlError).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string),
             typeof(string), typeof(int), typeof(int), typeof(Exception)])!;

        var errors = (SqlErrorCollection)typeof(SqlErrorCollection)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                [])!
            .Invoke(null)!;

        var error = (SqlError)errorCtor.Invoke(
            [number, (byte)0, (byte)0, "server", message, "procedure", 0, 0, null]);

        typeof(SqlErrorCollection).GetMethod("Add",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(errors, [error]);

        var sqlExCtor = typeof(SqlException).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            [typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid)])!;

        return (SqlException)sqlExCtor.Invoke([message, errors, null, Guid.NewGuid()]);
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private class TestDbException : DbException
    {
        public TestDbException() : base("Test database error") { }
    }

    private class SettableResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; }
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted { get; set; }

        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }
}
