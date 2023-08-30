using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.it;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer.UnitTest
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
                    { "ServerUrl", "https://localhost:8000" }
                }
            };
            sut.ConfigureServices(serviceCollection);


            var itsscd = serviceCollection.BuildServiceProvider().GetRequiredService<IITSSCD>();
        }
    }
}