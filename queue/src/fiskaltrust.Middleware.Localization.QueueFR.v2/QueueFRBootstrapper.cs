using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2;

/// <summary>
/// The v2 French localization. It replaces the receipt handling of the v1
/// <c>fiskaltrust.Middleware.Localization.QueueFR</c>: the NF525 signature moves out of the queue
/// into an SCU (SCU.FR.InfoCert or SCU.FR.LNE) and the queue keeps what is genuinely queue state -
/// the position of every receipt chain.
/// </summary>
public class QueueFRBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueFRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IFRSSCD frSSCD)
        : this(id, loggerFactory, configuration, frSSCD, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueueFRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IFRSSCD frSSCD, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueueFRAsync(id)).CashBoxIdentification);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();
        var pipeline = new FRSigningPipeline(frSSCD, new FRChainStateProvider(queueItemRepository));

        // FR launches with enforcing validation: the v2 localization has no legacy integrations to
        // stay compatible with, and the queue currency rule (EUR, rfcs/0705-queue-single-currency)
        // must reject rather than log. A configured ValidationLevel still takes precedence.
        var validationConfig = ValidationConfiguration.FromConfiguration(configuration);
        if (validationConfig.ValidationLevel is null)
        {
            validationConfig = new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = validationConfig.ValidationsInSignatures };
        }

        var receiptProcessorFR = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new ReceiptValidatorFR(new ReceiptReferenceProvider(queueItemRepository)),
            new LifecycleCommandProcessorFR(frSSCD, pipeline, queueStorageProvider),
            new ReceiptCommandProcessorFR(pipeline),
            new DailyOperationsCommandProcessorFR(pipeline),
            new InvoiceCommandProcessorFR(pipeline),
            new ProtocolCommandProcessorFR(pipeline),
            validationConfig);

        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, receiptProcessorFR.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorFR(), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
