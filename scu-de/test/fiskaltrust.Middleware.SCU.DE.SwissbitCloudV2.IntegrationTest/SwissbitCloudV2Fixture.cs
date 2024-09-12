using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    public sealed class SwissbitCloudV2Fixture
    {
        private SwissbitCloudV2SCU _instance;

        public SwissbitCloudV2SCUConfiguration Configuration { get; } = new SwissbitCloudV2SCUConfiguration();

        public string TestClientId { get; } = "client2";

        public SwissbitCloudV2SCU GetSut()
        {
            var swissbitCloudV2ApiProvider = new SwissbitCloudV2ApiProvider(Configuration);
            return _instance ?? (_instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(),
            swissbitCloudV2ApiProvider, new ClientCache(swissbitCloudV2ApiProvider), Configuration));
        }
    }
}
