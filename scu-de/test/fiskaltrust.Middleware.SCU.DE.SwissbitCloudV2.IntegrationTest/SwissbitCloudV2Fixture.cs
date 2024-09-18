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
    public sealed class SwissbitCloudV2Fixture
    {
        private SwissbitCloudV2SCU _instance;

        public SwissbitCloudV2SCUConfiguration Configuration { get; } = new SwissbitCloudV2SCUConfiguration()
        {
            TseSerialNumber = "fd79e44187bce2e2dcc886c89bf993df26d157503c4d953557b2e5af73571876",
            TseAccessToken = "6945c6ab69f348cd3779b5ee139466c4"
        };

        public string TestClientId { get; } = "TestClient";

        public async Task<SwissbitCloudV2SCU> GetSut() {

            if (_instance == null)
            {
               var apiProvider = new SwissbitCloudV2ApiProvider(Configuration, new HttpClientWrapper(Configuration, Mock.Of<ILogger<HttpClientWrapper>>()));
              _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
               await _instance.RegisterClientIdAsync(new ifPOS.v1.de.RegisterClientIdRequest() { ClientId = TestClientId });
            }

            return _instance;
        }

        public SwissbitCloudV2SCU GetNewSut()
        {

           var apiProvider = new SwissbitCloudV2ApiProvider(Configuration, new HttpClientWrapper(Configuration, Mock.Of<ILogger<HttpClientWrapper>>()));
            return _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
           
        }



    }
}
