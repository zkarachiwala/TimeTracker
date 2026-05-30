using Mapster;
using TimeTracker.Web.Features.TimeEntries;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

public sealed class MapsterFixture
{
    public MapsterFixture()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(TimeEntryMappingConfig).Assembly);
    }
}

[CollectionDefinition("Services")]
public class ServicesCollection : ICollectionFixture<MapsterFixture> { }
