using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Extensions;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES;


public class QueueESBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueESBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IESSSCD> clientFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueESConfiguration = QueueESConfiguration.FromMiddlewareConfiguration(middlewareConfiguration);

        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var masterDataService = new MasterDataService(configuration, storageProvider);

        var cashBoxIdentification = new Lazy<Task<string>>(async () => (await (await storageProvider.ConfigurationRepository.Value).GetQueueESAsync(id)).CashBoxIdentification);

        var essscd = new Lazy<Task<IESSSCD>>(async () =>
        {
            var configurationRepository = await storageProvider.ConfigurationRepository.Value;
            var queue = await configurationRepository.GetQueueESAsync(id);
            var scu = await configurationRepository.GetSignaturCreationUnitESAsync(queue.ftSignaturCreationUnitESId);
            return clientFactory.CreateClient(new ClientConfiguration
            {
                Timeout = queueESConfiguration.ScuTimeoutMs.HasValue ? TimeSpan.FromMilliseconds(queueESConfiguration.ScuTimeoutMs.Value) : TimeSpan.FromSeconds(15),
                RetryCount = queueESConfiguration.ScuMaxRetries.HasValue ? queueESConfiguration.ScuMaxRetries.Value : null,
                Url = scu.Url
            });
        });

        var signProcessorES = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new LifecycleCommandProcessorES(
                queueStorageProvider
            ),
            new ReceiptCommandProcessorES(
                essscd,
                storageProvider.ConfigurationRepository,
                storageProvider.MiddlewareQueueItemRepository.Cast<IMiddlewareQueueItemRepository, IReadOnlyQueueItemRepository>()
            ),
            new DailyOperationsCommandProcessorES(
                essscd,
                queueStorageProvider),
            new InvoiceCommandProcessorES(),
            new ProtocolCommandProcessorES()
        );
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorES.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorES(storageProvider.MiddlewareReceiptJournalRepository, storageProvider.MiddlewareQueueItemRepository, masterDataService), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(signProcessor, journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    public Func<string, Task<string>> RegisterForSign()
    {
        return _queue.RegisterForSign();
    }

    public Func<string, Task<string>> RegisterForEcho()
    {
        return _queue.RegisterForEcho();
    }

    public Func<string, Task<string>> RegisterForJournal()
    {
        return _queue.RegisterForJournal();
    }
}
