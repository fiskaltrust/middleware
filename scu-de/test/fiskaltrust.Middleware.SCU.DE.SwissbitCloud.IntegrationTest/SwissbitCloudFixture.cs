using System;
using System.IO;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud.IntegrationTest
{
    public sealed class SwissbitCloudFixture : IDisposable
    {
        private SwissbitCloudSCU _instance;

        private const string FCC_ID = "";
        public SwissbitCloudSCUConfiguration Configuration { get; } = new SwissbitCloudSCUConfiguration
        {
            FccId = FCC_ID,
            FccSecret = "",
            FccDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FCC", FCC_ID),
            ErsCode = "",
            ActivationToken = ""
        };

        public string TestClientId { get; } = "";

        public SwissbitCloudSCU GetSut() => _instance ?? (_instance = new SwissbitCloudSCU(Mock.Of<ILogger<DeutscheFiskalSCU>>(), Configuration,
            new DeutscheFiskalFccInitializationService(Configuration, Mock.Of<ILogger<DeutscheFiskalFccInitializationService>>(), new FirewallHelper()), new DeutscheFiskalFccProcessHost(Configuration, Mock.Of<ILogger<DeutscheFiskalFccProcessHost>>()),
            new DeutscheFiskalFccDownloadService(Configuration, Mock.Of<ILogger<IFccDownloadService>>()), new FccErsApiProvider(Configuration), new FccAdminApiProvider(Configuration)));


        public void Dispose() => _instance?.Dispose();
    }
}
