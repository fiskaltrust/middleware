using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Queue.Bootstrapper;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace fiskaltrust.Middleware.Queue.AcceptanceTest
{
    public class QueueBootstrapperStartupActionJournalTests
    {
        private readonly Guid _queueId = Guid.NewGuid();
        private readonly Guid _cashBoxId = Guid.NewGuid();

        [Fact]
        public void ConfigureServices_ShouldSetLauncherVersionOnMiddlewareConfiguration()
        {
            var config = CreateConfiguration(("launcherversion", "2.1.0"), ("launcherenvironment", LauncherEnvironments.Local));

            var (middlewareConfiguration, _) = ResolveMiddlewareConfiguration(config);

            middlewareConfiguration.LauncherVersion.Should().Be("2.1.0");
            middlewareConfiguration.LauncherEnvironment.Should().Be(LauncherEnvironments.Local);
        }

        [Theory]
        [InlineData("launcherversion")]
        [InlineData("LauncherVersion")]
        [InlineData("LAUNCHERVERSION")]
        public void ConfigureServices_ShouldReadLauncherVersion_CaseInsensitive(string key)
        {
            var config = CreateConfiguration((key, "2.1.0"));

            var (middlewareConfiguration, _) = ResolveMiddlewareConfiguration(config);

            middlewareConfiguration.LauncherVersion.Should().Be("2.1.0");
        }

        [Fact]
        public void ConfigureServices_WithoutLauncherVersion_ShouldSetLauncherVersionToNull()
        {
            var config = CreateConfiguration();

            var (middlewareConfiguration, actionJournals) = ResolveMiddlewareConfiguration(config);

            middlewareConfiguration.LauncherVersion.Should().BeNull();
            actionJournals.Should().ContainSingle();
            var dataJson = JObject.Parse(actionJournals.Single().DataJson);
            dataJson.Should().ContainKey(nameof(MiddlewareConfiguration.LauncherVersion));
            dataJson[nameof(MiddlewareConfiguration.LauncherVersion)].Type.Should().Be(JTokenType.Null);
        }

        [Fact]
        public void ConfigureServices_ShouldWriteStartupActionJournal_ContainingLauncherVersionAndEnvironment()
        {
            var config = CreateConfiguration(("launcherversion", "2.1.0"), ("launcherenvironment", LauncherEnvironments.Local));

            var (_, actionJournals) = ResolveMiddlewareConfiguration(config);

            actionJournals.Should().ContainSingle();
            var actionJournal = actionJournals.Single();
            actionJournal.ftQueueId.Should().Be(_queueId);
            actionJournal.ftQueueItemId.Should().Be(Guid.Empty);
            actionJournal.Type.Should().Be(nameof(MiddlewareConfiguration));
            actionJournal.Message.Should().Be("Queue started");
            actionJournal.Priority.Should().Be(0);

            var dataJson = JObject.Parse(actionJournal.DataJson);
            dataJson[nameof(MiddlewareConfiguration.LauncherVersion)].Value<string>().Should().Be("2.1.0");
            dataJson[nameof(MiddlewareConfiguration.LauncherEnvironment)].Value<string>().Should().Be(LauncherEnvironments.Local);
            dataJson[nameof(MiddlewareConfiguration.QueueId)].Value<string>().Should().Be(_queueId.ToString());
            dataJson[nameof(MiddlewareConfiguration.CashBoxId)].Value<string>().Should().Be(_cashBoxId.ToString());
        }

        [Fact]
        public void ConfigureServices_MigrationInProgress_NonCloud_ShouldNotWriteStartupActionJournal()
        {
            var config = CreateConfiguration(("launcherversion", "2.1.0"), ("launcherenvironment", LauncherEnvironments.Local));

            var (middlewareConfiguration, actionJournals) = ResolveMiddlewareConfiguration(config, migrationInProgress: true);

            middlewareConfiguration.LauncherVersion.Should().Be("2.1.0");
            actionJournals.Should().BeEmpty();
        }

        [Fact]
        public void ConfigureServices_MigrationInProgress_Cloud_ShouldStillWriteStartupActionJournal()
        {
            var config = CreateConfiguration(("launcherversion", "2.1.0"), ("launcherenvironment", LauncherEnvironments.Cloud));

            var (_, actionJournals) = ResolveMiddlewareConfiguration(config, migrationInProgress: true);

            actionJournals.Should().ContainSingle();
            var dataJson = JObject.Parse(actionJournals.Single().DataJson);
            dataJson[nameof(MiddlewareConfiguration.LauncherVersion)].Value<string>().Should().Be("2.1.0");
            dataJson[nameof(MiddlewareConfiguration.LauncherEnvironment)].Value<string>().Should().Be(LauncherEnvironments.Cloud);
        }

        private Dictionary<string, object> CreateConfiguration(params (string key, object value)[] additionalValues)
        {
            var queues = new List<ftQueue>
            {
                new ftQueue
                {
                    ftQueueId = _queueId,
                    ftCashBoxId = _cashBoxId,
                    CountryCode = "DE"
                }
            };

            var config = new Dictionary<string, object>
            {
                { "init_ftQueue", JsonConvert.SerializeObject(queues) },
                { "servicefolder", "C:/" }
            };

            foreach (var (key, value) in additionalValues)
            {
                config[key] = value;
            }

            return config;
        }

        private (MiddlewareConfiguration middlewareConfiguration, List<ftActionJournal> actionJournals) ResolveMiddlewareConfiguration(Dictionary<string, object> config, bool migrationInProgress = false)
        {
            var insertedActionJournals = new List<ftActionJournal>();

            var actionJournalRepository = new Mock<IMiddlewareActionJournalRepository>();
            actionJournalRepository
                .Setup(x => x.InsertAsync(It.IsAny<ftActionJournal>()))
                .Callback<ftActionJournal>(insertedActionJournals.Add)
                .Returns(Task.CompletedTask);

            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();

            if (migrationInProgress)
            {
                var migrationQueueItem = new ftQueueItem
                {
                    ftQueueItemId = Guid.NewGuid(),
                    ftQueueId = _queueId,
                    request = JsonConvert.SerializeObject(new ReceiptRequest { ftReceiptCase = 0x4445000000000019 })
                };
                queueItemRepository.Setup(x => x.GetLastQueueItemAsync()).ReturnsAsync(migrationQueueItem);
                actionJournalRepository
                    .Setup(x => x.GetByQueueItemId(migrationQueueItem.ftQueueItemId))
                    .Returns(ToAsyncEnumerable(new ftActionJournal { ftActionJournalId = Guid.NewGuid(), ftQueueItemId = migrationQueueItem.ftQueueItemId }));
            }
            else
            {
                queueItemRepository.Setup(x => x.GetLastQueueItemAsync()).ReturnsAsync((ftQueueItem) null);
            }

            var serviceCollection = new ServiceCollection();
            var sut = new QueueBootstrapper(_queueId, config, typeof(QueueBootstrapper));
            sut.ConfigureServices(serviceCollection);

            // Only resolve the MiddlewareConfiguration registration of the QueueBootstrapper, which creates the startup action journal
            var middlewareConfigurationDescriptor = serviceCollection.First(x => x.ServiceType == typeof(MiddlewareConfiguration));

            var provider = new ServiceCollection()
                .AddSingleton(queueItemRepository.Object)
                .AddSingleton(actionJournalRepository.Object)
                .BuildServiceProvider();

            var middlewareConfiguration = (MiddlewareConfiguration) middlewareConfigurationDescriptor.ImplementationFactory(provider);

            return (middlewareConfiguration, insertedActionJournals);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators
        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
#pragma warning restore CS1998
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}
