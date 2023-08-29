using Xunit;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Moq;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Models;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{


    public class ReceiptTypeProcessorFactoryTests
    {
        [Fact]
        public void CreateWithWrongReceiptCase_ShouldThrow()
        {

        }
    }

    public class QueueITBootstrapperTests
    {
        [Fact]
        public void TryToConstructSignProcessorIT()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton(Mock.Of<IConfigurationRepository>());
            serviceCollection.AddSingleton(Mock.Of<IJournalITRepository>());
            serviceCollection.AddSingleton(Mock.Of<IClientFactory<IITSSCD>>());
            serviceCollection.AddSingleton(new MiddlewareConfiguration
            {
                Configuration = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "init_ftSignaturCreationUnitIT", "[{\"Url\":\"https://faker\"}]" }
                }
            });

            var bootstrapper = new QueueITBootstrapper();
            bootstrapper.ConfigureServices(serviceCollection);

            serviceCollection.BuildServiceProvider().GetRequiredService<IMarketSpecificSignProcessor>();
        }  
    }

}
