using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueIT.Models;
using fiskaltrust.Middleware.Localization.QueueIT.Processors;
using fiskaltrust.Middleware.Localization.QueueIT.SCU;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT;

/// <summary>
/// Wires the Italian localization onto the v2 stream. The SCU is reached through the launcher's
/// <see cref="IClientFactory{T}"/> for <see cref="IITSSCD"/>, the only contract the Italian RT devices and
/// servers implement; see <see cref="ITSSCDProvider"/> for the v1/v2 translation.
/// </summary>
public class QueueITBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueITBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IITSSCD> clientFactory, Dictionary<string, object> configuration)
        : this(id, loggerFactory, clientFactory, configuration, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueueITBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IITSSCD> clientFactory, Dictionary<string, object> configuration, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueITConfiguration = QueueITConfiguration.FromMiddlewareConfiguration(middlewareConfiguration);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var configurationRepository = storageProvider.CreateConfigurationRepository();
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();
        var journalITRepository = storageProvider.CreateMiddlewareJournalITRepository();

        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await configurationRepository).GetQueueITAsync(id)).CashBoxIdentification);
        var sscd = new ITSSCDProvider(loggerFactory.CreateLogger<ITSSCDProvider>(), clientFactory, configurationRepository, id, queueITConfiguration);

        var receiptProcessor = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new ReceiptReferenceProvider(queueItemRepository),
            new LifecycleCommandProcessorIT(sscd, queueStorageProvider, configurationRepository),
            new ReceiptCommandProcessorIT(sscd, journalITRepository, configurationRepository),
            new DailyOperationsCommandProcessorIT(sscd, journalITRepository, configurationRepository),
            new InvoiceCommandProcessorIT(),
            new ProtocolCommandProcessorIT(sscd),
            ValidationConfiguration.FromConfiguration(configuration));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, receiptProcessor.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorIT(), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal()
    {
        return _queue.RegisterForJournal();
    }
}
