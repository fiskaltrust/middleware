using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.FiskalyCertified;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.AcceptanceTest.SCU
{
    public class FiskalyDESSCDTests : IDESSCDTests
    {
        protected override IDESSCD GetResetSystemUnderTest(Dictionary<string, object> configuration = null) => throw new System.NotImplementedException();

        protected override IDESSCD GetSystemUnderTest(Dictionary<string, object> configuration = null)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var scuBootStrapper = new ScuBootstrapper
            {
                Configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(new FiskalySCUConfiguration
                {
                    // TODO SETTINGS
                }))
            };
            scuBootStrapper.ConfigureServices(serviceCollection);
            return serviceCollection.BuildServiceProvider().GetService<IDESSCD>();
        }
    }
}
