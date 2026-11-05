using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.QueueSynchronizer;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{
    public class QueueBootstrapperUnitTests
    {
        [Fact]
        public void QueueBootstrapper_AddQueueServices_ShouldReturnServiceCollection()
        {
            var queueId = Guid.NewGuid();
            var cashBoxId = Guid.NewGuid();
            var queues = new List<ftQueue>()
            {
                new ftQueue()
                {
                    ftQueueId = Guid.NewGuid(),
                    ftCashBoxId = Guid.NewGuid(),
                    CountryCode = "AT"
                },
                new ftQueue()
                {
                    ftQueueId = queueId,
                    ftCashBoxId = cashBoxId,
                    CountryCode = "DE"
                }
            };
            var config = new Dictionary<string, object>()
            {
                { "init_ftQueue", JsonConvert.SerializeObject(queues) },
                { "servicefolder", "C:/" }
            };
            var signProcessorConfig = new MiddlewareConfiguration()
            {
                QueueId = queueId,
                CashBoxId = cashBoxId,
                ServiceFolder = "C:/",
                Configuration = config,
                ProcessingVersion = "test"
            };
            var serviceCollection = new ServiceCollection();

            var sut = new QueueBootstrapper(queueId, config, typeof(QueueBootstrapper));
            sut.ConfigureServices(serviceCollection);

            serviceCollection.Should().HaveCount(38);

            var cryptoHelper = new ServiceDescriptor(typeof(ICryptoHelper), typeof(CryptoHelper), ServiceLifetime.Scoped);
            var signProcessorDecorator = new ServiceDescriptor(typeof(ISignProcessor), x => new LocalQueueSynchronizationDecorator(x.GetRequiredService<ISignProcessor>(), x.GetRequiredService<ILogger<LocalQueueSynchronizationDecorator>>()), ServiceLifetime.Scoped);
            var signProcessor = new ServiceDescriptor(typeof(SignProcessor), typeof(SignProcessor), ServiceLifetime.Scoped);
            var journalProcessor = new ServiceDescriptor(typeof(IJournalProcessor), typeof(JournalProcessor), ServiceLifetime.Scoped);
            var iPos = new ServiceDescriptor(typeof(IPOS), typeof(Queue), ServiceLifetime.Scoped);

            serviceCollection.Should().ContainEquivalentOf(cryptoHelper);
            serviceCollection.Should().ContainEquivalentOf(signProcessor);
            serviceCollection.Should().ContainEquivalentOf(signProcessorDecorator, options => options.Excluding(su => su.ImplementationFactory));
            serviceCollection.Should().ContainEquivalentOf(journalProcessor);
            serviceCollection.Should().ContainEquivalentOf(iPos);
            serviceCollection.Should().ContainEquivalentOf(new ServiceDescriptor(typeof(IEmptyDatabaseCheck), typeof(EmptyDatabaseCheck), ServiceLifetime.Singleton));
        }

        [Fact]
        public void QueueBootstrapper_ShouldKeepEmptyDatabaseCheck_RegisteredByStorage()
        {
            var queueId = Guid.NewGuid();
            var config = new Dictionary<string, object>()
            {
                { "init_ftQueue", JsonConvert.SerializeObject(new List<ftQueue> { new ftQueue { ftQueueId = queueId, ftCashBoxId = Guid.NewGuid(), CountryCode = "DE" } }) },
                { "servicefolder", "C:/" }
            };
            var serviceCollection = new ServiceCollection();
            var storageCheck = new Mock<IEmptyDatabaseCheck>().Object;
            serviceCollection.AddSingleton(storageCheck);

            new QueueBootstrapper(queueId, config, typeof(QueueBootstrapper)).ConfigureServices(serviceCollection);

            serviceCollection.Where(x => x.ServiceType == typeof(IEmptyDatabaseCheck)).Should().ContainSingle()
                .Which.ImplementationInstance.Should().BeSameAs(storageCheck);
        }
    }
}
