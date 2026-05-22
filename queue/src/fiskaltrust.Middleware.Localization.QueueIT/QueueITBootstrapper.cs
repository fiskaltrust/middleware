using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT;

public class QueueITBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueITBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IITSSCD itSSCD)
        : this(id, loggerFactory, configuration, itSSCD, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueueITBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IITSSCD itSSCD, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueIT = JsonConvert.DeserializeObject<List<ftQueueIT>>(configuration["init_ftQueueIT"]!.ToString()!)!.First();

        var configurationRepository = storageProvider.CreateConfigurationRepository().Value.GetAwaiter().GetResult();
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository().Value.GetAwaiter().GetResult();
        var journalITRepository = storageProvider.CreateMiddlewareJournalITRepository().Value.GetAwaiter().GetResult();
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var cashBoxIdentification = new AsyncLazy<string>(() => Task.FromResult(queueIT.CashBoxIdentification));

        var receiptProcessor = new ReceiptCommandProcessorIT(itSSCD, journalITRepository, queueItemRepository, queueIT);
        var lifecycleProcessor = new LifecycleCommandProcessorIT(itSSCD, configurationRepository, queueIT);
        var dailyOperationsProcessor = new DailyOperationsCommandProcessorIT(itSSCD, configurationRepository, journalITRepository, queueIT);
        var invoiceProcessor = new InvoiceCommandProcessorIT();
        var protocolProcessor = new ProtocolCommandProcessorIT(itSSCD, queueItemRepository);

        var signProcessorIT = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new ReceiptReferenceProvider(storageProvider.CreateMiddlewareQueueItemRepository()),
            lifecycleProcessor,
            receiptProcessor,
            dailyOperationsProcessor,
            invoiceProcessor,
            protocolProcessor);

        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorIT.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorIT(), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(signProcessor, journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    public Func<string, Task<string>> RegisterForSign() => _queue.RegisterForSign();

    public Func<string, Task<string>> RegisterForEcho() => _queue.RegisterForEcho();

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal() => _queue.RegisterForJournal();
}
