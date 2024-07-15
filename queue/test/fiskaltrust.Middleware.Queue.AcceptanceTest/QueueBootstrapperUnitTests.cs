using System;
using System.Collections.Generic;
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
                Configuration = config
            };
            var serviceCollection = new ServiceCollection();

            var sut = new QueueBootstrapper(queueId, config);
            sut.ConfigureServices(serviceCollection);

            serviceCollection.Should().HaveCount(37);

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
        }
    }
}
