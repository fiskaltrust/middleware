using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using EpsonConfiguration = fiskaltrust.Middleware.SCU.DE.Epson.EpsonConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest.SCU
{
    public class EpsonDESSCDTests : IDESSCDTests
    {
        protected override IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null) => throw new NotImplementedException();

        protected override IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new Epson.ScuBootstrapper
            {
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(new EpsonConfiguration
                {
                    Host = "127.0.0.1",
                    Port = 8009,
                    DeviceId = "local_TSE"
                }))
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetService<IDESSCD>();
        }
    }
}
