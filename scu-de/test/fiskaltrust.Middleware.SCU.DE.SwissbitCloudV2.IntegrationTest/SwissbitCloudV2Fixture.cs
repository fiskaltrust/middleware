using System;
using System.IO;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud.IntegrationTest
{
    public sealed class SwissbitCloudV2Fixture
    {
        private SwissbitCloudV2SCU _instance;

        public SwissbitCloudV2SCUConfiguration Configuration { get; } = new SwissbitCloudV2SCUConfiguration();

        public string TestClientId { get; } = "TestClient";

        public SwissbitCloudV2SCU GetSut() => _instance ?? (_instance = new SwissbitCloudV2SCU(Mock.Of<ILogger<SwissbitCloudV2SCU>>(),
            new SwissbitCloudV2ApiProvider(Configuration), new ClientCache(), Configuration));


    }
}
