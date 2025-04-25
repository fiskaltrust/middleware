using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using fiskaltrust.Middleware.Queue.InMemory.IntegrationTest.Configuration;
using fiskaltrust.Middleware.Storage.InMemory;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.ifPOS.v1.de;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueDE.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDE;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Queue.InMemory.IntegrationTest
{
    public class PosBootstrapperTests
    {
        [Fact]
        public void MemoryRepositoriesTest()
        {
            var configuration = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            var json = File.ReadAllText(configuration);
            var values = JsonConvert.DeserializeObject<ConfigFile>(json);

            var dictionary = new Dictionary<string, object>
            {
                { "init_ftQueueAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueAT) },
                { "init_ftQueueDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueDE) },
                { "init_ftQueueFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueFR) },
                { "init_ftSignaturCreationUnitAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitAT) },
                { "init_ftSignaturCreationUnitDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitDE) },
                { "init_ftSignaturCreationUnitFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitFR) },
                { "init_ftCashBox", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftCashBox) },
                { "init_ftQueue", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueue) }
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            var storageBootStrapper = new InMemoryStorageBootstrapper(values.ftQueues[0].Id, dictionary, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            storageBootStrapper.ConfigureStorageServices(serviceCollection);

            serviceCollection.Count.Should().Be(48);

            CheckServiceType(serviceCollection, typeof(IConfigurationRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyConfigurationRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IQueueItemRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyQueueItemRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IJournalATRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyJournalATRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IJournalFRRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyJournalFRRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IJournalDERepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyJournalDERepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReceiptJournalRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyReceiptJournalRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IActionJournalRepository)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IReadOnlyActionJournalRepository)).Should().BeTrue();
        }

        [Fact]
        public void QueueServicesTest()
        {
            var configuration = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            var json = File.ReadAllText(configuration);
            var values = JsonConvert.DeserializeObject<ConfigFile>(json);

            var dictionary = new Dictionary<string, object>
            {
                { "init_ftQueueAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueAT) },
                { "init_ftQueueDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueDE) },
                { "init_ftQueueFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueFR) },
                { "init_ftSignaturCreationUnitAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitAT) },
                { "init_ftSignaturCreationUnitDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitDE) },
                { "init_ftSignaturCreationUnitFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitFR) },
                { "init_ftCashBox", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftCashBox) },
                { "init_ftQueue", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueue) }
            };

            var serviceCollection = new ServiceCollection();

            var queueBootstrapper = new QueueBootstrapper(values.ftQueues[0].Id, dictionary, typeof(QueueBootstrapper));
            queueBootstrapper.ConfigureServices(serviceCollection);


            serviceCollection.Count.Should().Be(36);

            CheckServiceType(serviceCollection, typeof(ICryptoHelper)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(ISignProcessor)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IJournalProcessor)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(IPOS)).Should().BeTrue();
            CheckServiceType(serviceCollection, typeof(MiddlewareConfiguration)).Should().BeTrue();
        }

        private bool CheckServiceType(IServiceCollection serviceCollection, Type type)
        {
            foreach (var item in serviceCollection)
            {
                if (item.ServiceType == type)
                {
                    return true;
                }
            }

            return false;
        }

        [Fact]
        public void PosInstanceTest()
        {
            var configuration = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            var json = File.ReadAllText(configuration);
            var values = JsonConvert.DeserializeObject<ConfigFile>(json);

            var dictionary = new Dictionary<string, object>
            {
                { "init_ftQueueAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueAT) },
                { "init_ftQueueDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueDE) },
                { "init_ftQueueFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueFR) },
                { "init_ftSignaturCreationUnitAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitAT) },
                { "init_ftSignaturCreationUnitDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitDE) },
                { "init_ftSignaturCreationUnitFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitFR) },
                { "init_ftCashBox", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftCashBox) },
                { "init_ftQueue", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueue) }
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddScoped(x => Mock.Of<IClientFactory<IDESSCD>>());
            serviceCollection.AddScoped(x => Mock.Of<IRequestCommandFactory>());

            var posBootstrapper = new PosBootstrapper
            {
                Id = values.ftQueues[0].Id,
                Configuration = dictionary
            };
            posBootstrapper.ConfigureServices(serviceCollection);

            serviceCollection.BuildServiceProvider().GetService<IPOS>().Should().NotBeNull();
        }

        [Fact]
        public void BootStrapperQueue_Should_Be_QueueDE()
        {
            var configuration = Path.Combine(Directory.GetCurrentDirectory(), "configuration.json");
            var json = File.ReadAllText(configuration);
            var values = JsonConvert.DeserializeObject<ConfigFile>(json);

            var config = new Dictionary<string, object>
            {
                { "init_ftQueueAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueAT) },
                { "init_ftQueueDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueDE) },
                { "init_ftQueueFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueueFR) },
                { "init_ftSignaturCreationUnitAT", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitAT) },
                { "init_ftSignaturCreationUnitDE", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitDE) },
                { "init_ftSignaturCreationUnitFR", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftSignaturCreationUnitFR) },
                { "init_ftCashBox", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftCashBox) },
                { "init_ftQueue", JsonConvert.SerializeObject(values.ftQueues[0].Configuration.init_ftQueue) }
            };

            var businessLogicFactoryBoostrapper = LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(values.ftQueues[0].Configuration.init_ftQueue[0].ftQueueId, new MiddlewareConfiguration
            {
                Configuration = config,
                PreviewFeatures = new(),
                ProcessingVersion = "test"
            });
            businessLogicFactoryBoostrapper.Should().BeOfType(typeof(QueueDEBootstrapper));
        }
    }
}
