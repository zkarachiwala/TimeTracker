namespace TimeTracker.Web.Features.AwardRate;

public interface IAwardRateResolver
{
    (decimal? EffectiveRate, bool IsAwardRate) Resolve(DateTime entryDate, decimal? projectRate, decimal? clientAwardRate);
}
