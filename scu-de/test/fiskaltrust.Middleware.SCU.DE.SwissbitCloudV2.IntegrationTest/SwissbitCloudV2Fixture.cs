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
            TseSerialNumber = "12debb7976d3c5c49a8482f2742acc25ddd7244fe69e4b754559dc53b8166a27",
            TseAccessToken = "4cba251e128368b7fa1abc5ce87c6dd4"
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
