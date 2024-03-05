using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter.UnitTest
{
    public class ScuBootstrapperTests
    {
        [Fact]
        public void Test1()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();

            var sut = new ScuBootstrapper
            {
                Id = Guid.NewGuid(),
                Configuration = new Dictionary<string, object>
                {
                    { "DeviceUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);

            _ = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }
    }
}