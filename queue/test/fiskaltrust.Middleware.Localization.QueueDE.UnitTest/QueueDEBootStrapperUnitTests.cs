using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.Middleware.Queue.Bootstrapper.Interfaces;
using fiskaltrust.Middleware.Queue.Bootstrapper.Localization;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class QueueDEBootStrapperUnitTests
    {
        [Fact]
        public void QueueBootStrapperFactory_ShouldReturnQueueDEBootstrapper()
        {
            var queueId = Guid.NewGuid();
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
                    ftCashBoxId = Guid.NewGuid(),
                    CountryCode = "DE"
                }
            };

            var config = new Dictionary<string, object>()
            {
                { "init_ftQueue", JsonConvert.SerializeObject(queues) }
            };

            var queueDEBootStrapper = LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(queueId, config);
            queueDEBootStrapper.Should().BeOfType(typeof(QueueDEBootstrapper));
            queueDEBootStrapper.GetType().Should().Implement(typeof(ILocalizedQueueBootstrapper));
        }

        [Fact]
        public void QueueDEBootstrapper_AddLocalization_ShouldReturnServiceCollection()
        {
            var serviceCollection = new ServiceCollection();

            var sut = new QueueDEBootstrapper();
            sut.ConfigureServices(serviceCollection);

            serviceCollection.Should().HaveCount(27);

            var tranactionPayloadFactory = new ServiceDescriptor(typeof(ITransactionPayloadFactory), typeof(DSFinVKTransactionPayloadFactory), ServiceLifetime.Scoped);
            var signProcessorDE = new ServiceDescriptor(typeof(IMarketSpecificSignProcessor), typeof(SignProcessorDE), ServiceLifetime.Scoped);

            serviceCollection.Should().ContainEquivalentOf(tranactionPayloadFactory);
            serviceCollection.Should().ContainEquivalentOf(signProcessorDE);
        }
    }
}
