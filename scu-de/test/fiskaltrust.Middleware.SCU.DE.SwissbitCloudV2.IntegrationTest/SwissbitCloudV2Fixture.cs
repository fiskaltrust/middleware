using System;
using System.Threading.Tasks;
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
            TseSerialNumber = "ce40a6b871ce58588f3003995ab10d64b394d47e62b6c254405d8080bb9ccfbd",
            TseAccessToken = "eecd2ba410d40299d2a94cdb11d1c05b",
            Sandbox = true

        };

        public string TestClientId { get; } = "TestClient";

        public async Task<SwissbitCloudV2SCU> GetSut() {

            if (_instance == null)
            {
               var apiProvider = new SwissbitCloudV2ApiProvider(Configuration, new HttpClientWrapper(Configuration, Mock.Of<ILogger<HttpClientWrapper>>()), Mock.Of<ILogger<SwissbitCloudV2ApiProvider>>());
              _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
               await _instance.RegisterClientIdAsync(new ifPOS.v1.de.RegisterClientIdRequest() { ClientId = TestClientId });
            }

            return _instance;
        }

        public SwissbitCloudV2SCU GetNewSut()
        {
           var apiProvider = new SwissbitCloudV2ApiProvider(Configuration, new HttpClientWrapper(Configuration, Mock.Of<ILogger<HttpClientWrapper>>()), Mock.Of<ILogger<SwissbitCloudV2ApiProvider>>());
            return _instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(), apiProvider, new ClientCache(apiProvider), Configuration);
        }
    }
}
