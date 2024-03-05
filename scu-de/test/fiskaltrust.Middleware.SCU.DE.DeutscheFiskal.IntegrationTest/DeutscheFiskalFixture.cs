using System;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.IntegrationTest
{
    public sealed class DeutscheFiskalFixture : IDisposable
    {
        private DeutscheFiskalSCU _instance;
        private readonly DeutscheFiskalSCUConfiguration _configuration = new DeutscheFiskalSCUConfiguration
        {
            FccId = "",
            FccSecret = "",
            FccDirectory = "",
            ErsCode = "",
            ActivationToken = "",
            FccPort = 20002
        };

        public string TestClientId { get; } = "";

        public DeutscheFiskalSCU GetSut() => _instance ?? (_instance = new DeutscheFiskalSCU(Mock.Of<ILogger<DeutscheFiskalSCU>>(), _configuration,
            new DeutscheFiskalFccInitializationService(_configuration, Mock.Of<ILogger<DeutscheFiskalFccInitializationService>>(), new FirewallHelper()), new DeutscheFiskalFccProcessHost( new FccAdminApiProvider(_configuration, Mock.Of<ILogger<FccAdminApiProvider>>()), _configuration, Mock.Of<ILogger<DeutscheFiskalFccProcessHost>>()),
            new DeutscheFiskalFccDownloadService(_configuration, Mock.Of<ILogger<DeutscheFiskalFccDownloadService>>()), new FccErsApiProvider(_configuration), new FccAdminApiProvider(_configuration, Mock.Of<ILogger<FccAdminApiProvider>>())));

        public void Dispose() => _instance?.Dispose();
    }
}
