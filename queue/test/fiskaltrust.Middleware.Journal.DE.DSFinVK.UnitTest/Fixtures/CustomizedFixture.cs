using System;
using AutoFixture;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures
{
    public static class CustomizedFixture
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
