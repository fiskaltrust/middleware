using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Commands;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Communication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

#pragma warning disable
namespace fiskaltrust.Middleware.SCU.DE.Test.Tools
{
    public class Program
    { 
        public static void Main()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var provider = serviceCollection.BuildServiceProvider();

            DieboldNixdorf_PerformResetWithSelfTest(provider.GetRequiredService<ILoggerFactory>(), "COM3");
        }

        public static void DieboldNixdorf_PerformResetWithSelfTest(ILoggerFactory loggerFactory, string comPort)
        {
            var serialPortCommunicationProvider = new SerialPortCommunicationQueue(loggerFactory.CreateLogger<SerialPortCommunicationQueue>(), comPort, 1500, 1500, true);
            var tseCommunicationCommandHelper = new TseCommunicationCommandHelper(loggerFactory.CreateLogger<TseCommunicationCommandHelper>(), serialPortCommunicationProvider, 1);
            var authenticationTseCommandProvider = new AuthenticationTseCommandProvider(loggerFactory.CreateLogger<AuthenticationTseCommandProvider>(), tseCommunicationCommandHelper);
            var standardTseCommandsProvider = new StandardTseCommandsProvider(tseCommunicationCommandHelper);
            standardTseCommandsProvider.DisableAsb();
            var developerTseCommandProvider = new DeveloperTseCommandsProvider(tseCommunicationCommandHelper);
            developerTseCommandProvider.FactoryReset();
            var adminTseCommandFactory = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            authenticationTseCommandProvider.ExecuteAuthorized("1", "12345", () => adminTseCommandFactory.RegisterClient("DN TSEProduction ef82abcedf"));
            var maintenanceTseCommandProvider = new MaintenanceTseCommandProvider(tseCommunicationCommandHelper, Mock.Of<ILogger<MaintenanceTseCommandProvider>>());
            maintenanceTseCommandProvider.RunSelfTest("DN TSEProduction ef82abcedf");
        }
    }
}
