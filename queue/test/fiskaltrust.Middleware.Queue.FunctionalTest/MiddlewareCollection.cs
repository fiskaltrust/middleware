using Xunit;

namespace fiskaltrust.Middleware.Queue.FunctionalTest
{
    [CollectionDefinition("Middleware Collection")]
    public class MiddlewareCollection : ICollectionFixture<MiddlewareFixture> { }
}