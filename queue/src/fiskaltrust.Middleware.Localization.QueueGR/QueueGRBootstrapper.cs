using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2.gr;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueGRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IGRSSCD grSSCD)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var signaturCreationUnitGR = new ftSignaturCreationUnitGR();


        //var storageProvider = new AzureStorageProvider(loggerFactory, id, Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonSerializer.Serialize(configuration)));
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueueGRAsync(id)).CashBoxIdentification);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var configurationRepository = storageProvider.CreateConfigurationRepository();
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();

        // One-time invoice-counter migration, started eagerly at queue construction and
        // awaited before every receipt. If the counter cannot be initialized, receipt
        // processing fails loudly instead of silently falling back to the legacy
        // ftReceiptNumerator-derived numbering; a faulted attempt (e.g. a transient
        // storage error at startup) is retried on the next receipt.
        var migrationLogger = loggerFactory.CreateLogger<QueueGRBootstrapper>();
        var invoiceCounterMigration = new InvoiceCounterMigrationGate(async () =>
            await InvoiceCounterMigration.EnsureMigratedAsync(await configurationRepository, await queueItemRepository, id, migrationLogger));

        var signProcessorGR = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new ReceiptReferenceProvider(queueItemRepository), new LifecycleCommandProcessorGR(queueStorageProvider, configurationRepository), new ReceiptCommandProcessorGR(grSSCD, queueStorageProvider, configurationRepository, loggerFactory.CreateLogger<ReceiptCommandProcessorGR>()), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(grSSCD, queueStorageProvider, configurationRepository, loggerFactory.CreateLogger<InvoiceCommandProcessorGR>()), new ProtocolCommandProcessorGR(grSSCD, queueStorageProvider, configurationRepository, loggerFactory.CreateLogger<ProtocolCommandProcessorGR>()));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, async (request, response, queue, queueItem) =>
        {
            await invoiceCounterMigration.EnsureMigratedAsync();
            return await signProcessorGR.ProcessAsync(request, response, queue, queueItem);
        }, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorGR(storageProvider, GetFromConfig(configuration) ?? new MasterDataConfiguration { }), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(signProcessor, journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    public MasterDataConfiguration? GetFromConfig(Dictionary<string, object> configuration)
    {
        return configuration.ContainsKey("init_masterData") ? JsonConvert.DeserializeObject<MasterDataConfiguration>(configuration["init_masterData"].ToString()!) : null;
    }

    public Func<string, Task<string>> RegisterForSign()
    {
        return _queue.RegisterForSign();
    }

    public Func<string, Task<string>> RegisterForEcho()
    {
        return _queue.RegisterForEcho();
    }

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal()
    {
        return _queue.RegisterForJournal();
    }
}
