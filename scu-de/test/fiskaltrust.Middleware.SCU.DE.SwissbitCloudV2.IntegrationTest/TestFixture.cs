using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    public sealed class TestFixture
    {
        private SwissbitCloudV2SCU _instance;

        public SwissbitCloudV2SCUConfiguration Configuration { get; } = new SwissbitCloudV2SCUConfiguration();

        public string TestClientId { get; } = "TestClient";

        public async Task<SwissbitCloudV2SCU> GetSut() {

            if (_instance == null)
            {
               var apiProvider = new SwissbitCloudV2ApiProvider(Configuration);
              _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
               await _instance.RegisterClientIdAsync(new ifPOS.v1.de.RegisterClientIdRequest() { ClientId = TestClientId });
            }

            return _instance;
        }

        public SwissbitCloudV2SCU GetNewSut()
        {

           var apiProvider = new SwissbitCloudV2ApiProvider(Configuration);
           return _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
           
        }



    }
}
