using TimeTracker.Web.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

public class DevEndpointExtensionsTests
{
    private const string ExpectedToken = "correct-token-value";

    [Fact]
    public void IsValidToken_ReturnsTrue_ForMatchingToken()
    {
        Assert.True(DevEndpointExtensions.IsValidToken(ExpectedToken, ExpectedToken));
    }

    [Fact]
    public void IsValidToken_ReturnsFalse_ForMismatchedToken()
    {
        Assert.False(DevEndpointExtensions.IsValidToken("wrong-token-value", ExpectedToken));
    }

    [Fact]
    public void IsValidToken_ReturnsFalse_ForDifferentLengthToken()
    {
        Assert.False(DevEndpointExtensions.IsValidToken("short", ExpectedToken));
    }

    [Fact]
    public void IsValidToken_ReturnsFalse_ForEmptyProvidedToken()
    {
        Assert.False(DevEndpointExtensions.IsValidToken("", ExpectedToken));
    }
}
