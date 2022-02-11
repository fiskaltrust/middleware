using System;
using AutoFixture;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public static class StorageTestFixtureProvider
    {
        public static Fixture GetFixture()
        {
            var fixture = new Fixture();
            fixture.Inject(DateTime.UtcNow);
            fixture.Customize<decimal>(c => c.FromFactory<int>(i => i * 1.33M));

            return fixture;
        }
    }
}