using Xunit;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest.Fixtures
{
    [CollectionDefinition("AzureTableStorageCollection")]
    public class AzureTableStorageFixtureCollection : ICollectionFixture<AzureTableStorageFixture>
    {
    }
}
