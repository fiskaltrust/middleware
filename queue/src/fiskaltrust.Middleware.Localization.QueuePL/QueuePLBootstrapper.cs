using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.QueuePL.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePL;

public class QueuePLBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueuePLBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IPLSSCD plSSCD)
        : this(id, loggerFactory, configuration, plSSCD, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueuePLBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IPLSSCD plSSCD, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueuePLAsync(id)).CashBoxIdentification);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();

        var signProcessorPL = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new ReceiptReferenceProvider(queueItemRepository),
            new LifecycleCommandProcessorPL(plSSCD, queueStorageProvider),
            new ReceiptCommandProcessorPL(plSSCD),
            new DailyOperationsCommandProcessorPL(plSSCD),
            new InvoiceCommandProcessorPL(),
            new ProtocolCommandProcessorPL());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPL.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorPL(), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
