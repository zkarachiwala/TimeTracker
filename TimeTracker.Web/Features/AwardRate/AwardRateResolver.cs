using PublicHoliday;

namespace TimeTracker.Web.Features.AwardRate;

public class AwardRateResolver : IAwardRateResolver
{
    private static readonly AustraliaPublicHoliday _auHolidays = new();

    public (decimal? EffectiveRate, bool IsAwardRate) Resolve(DateTime entryDate, decimal? projectRate, decimal? clientAwardRate)
    {
        if (clientAwardRate.HasValue && IsWeekendOrHoliday(entryDate))
            return (clientAwardRate, true);

        return (projectRate, false);
    }

    private static bool IsWeekendOrHoliday(DateTime date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return true;

        return _auHolidays.IsPublicHoliday(date);
    }
}
