using TimeTracker.Web.Features.AwardRate;
using Xunit;

namespace TimeTracker.Tests.Features.AwardRate;

[Collection("Services")]
public class AwardRateResolverTests
{
    private readonly AwardRateResolver _resolver = new();

    // 2026-01-01 (Thursday) is New Year's Day — national AU public holiday
    private static readonly DateTime AuPublicHoliday = new(2026, 1, 1);

    // 2026-06-20 is a Saturday
    private static readonly DateTime Saturday = new(2026, 6, 20);

    // 2026-06-21 is a Sunday
    private static readonly DateTime Sunday = new(2026, 6, 21);

    // 2026-06-18 is a Thursday — regular weekday, not a holiday
    private static readonly DateTime Weekday = new(2026, 6, 18);

    [Fact]
    public void Weekday_NonHoliday_ReturnsProjectRate_NotAward()
    {
        var (rate, isAward) = _resolver.Resolve(Weekday, 100m, 150m);

        Assert.Equal(100m, rate);
        Assert.False(isAward);
    }

    [Fact]
    public void Saturday_WithAwardRate_ReturnsAwardRate_IsAwardTrue()
    {
        var (rate, isAward) = _resolver.Resolve(Saturday, 100m, 150m);

        Assert.Equal(150m, rate);
        Assert.True(isAward);
    }

    [Fact]
    public void Sunday_WithAwardRate_ReturnsAwardRate_IsAwardTrue()
    {
        var (rate, isAward) = _resolver.Resolve(Sunday, 100m, 150m);

        Assert.Equal(150m, rate);
        Assert.True(isAward);
    }

    [Fact]
    public void AuPublicHoliday_WithAwardRate_ReturnsAwardRate_IsAwardTrue()
    {
        var (rate, isAward) = _resolver.Resolve(AuPublicHoliday, 100m, 150m);

        Assert.Equal(150m, rate);
        Assert.True(isAward);
    }

    [Fact]
    public void Weekend_AwardRateNull_ReturnsProjectRate_NotAward()
    {
        var (rate, isAward) = _resolver.Resolve(Saturday, 100m, null);

        Assert.Equal(100m, rate);
        Assert.False(isAward);
    }

    [Fact]
    public void AuPublicHoliday_AwardRateNull_ReturnsProjectRate_NotAward()
    {
        var (rate, isAward) = _resolver.Resolve(AuPublicHoliday, 100m, null);

        Assert.Equal(100m, rate);
        Assert.False(isAward);
    }

    [Fact]
    public void Weekday_BothRatesNull_ReturnsNullRate_NotAward()
    {
        var (rate, isAward) = _resolver.Resolve(Weekday, null, null);

        Assert.Null(rate);
        Assert.False(isAward);
    }

    [Fact]
    public void Weekend_ProjectRateNull_AwardRateSet_ReturnsAwardRate_IsAwardTrue()
    {
        var (rate, isAward) = _resolver.Resolve(Saturday, null, 150m);

        Assert.Equal(150m, rate);
        Assert.True(isAward);
    }
}
