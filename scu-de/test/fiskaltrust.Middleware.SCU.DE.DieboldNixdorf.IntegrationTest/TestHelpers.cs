using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using Microsoft.Extensions.Logging;
using Moq;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.IntegrationTest
{
    public static class TestHelpers
    {
        public static void PerformResetWithSelfTest(SerialPortCommunicationQueue serialPortCommunicationProvider)
        {

            var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), serialPortCommunicationProvider, 1);
            var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), tseCommunicationCommandHelper);
            var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
            standardTseCommandsProvider.DisableAsb();
            var developerTseCommandProvider = new DeveloperTseCommandsProvider(tseCommunicationCommandHelper);
            developerTseCommandProvider.FactoryReset();
            var adminTseCommandFactory = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            authenticationTseCommandProvider.ExecuteAuthorized("1", "12345", () => adminTseCommandFactory.RegisterClient("DN TSEProduction ef82abcedf"));
            var maintenanceTseCommandProvider = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
        }

        public static void PerformReset(SerialPortCommunicationQueue serialPortCommunicationProvider)
        {
            var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(Mock.Of<ILogger<TseCommunicationCommandHelper>>(), serialPortCommunicationProvider, 1);
            var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(Mock.Of<ILogger<AuthenticationTseCommandProvider>>(), tseCommunicationCommandHelper);
            var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
            standardTseCommandsProvider.DisableAsb();
            var developerTseCommandProvider = new DeveloperTseCommandsProvider(tseCommunicationCommandHelper);
            developerTseCommandProvider.FactoryReset();
            var adminTseCommandFactory = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            authenticationTseCommandProvider.ExecuteAuthorized("1", "12345", () => adminTseCommandFactory.RegisterClient( "DN TSEProduction ef82abcedf"));
        }
    }
}