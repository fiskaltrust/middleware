using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.Base.Interface;

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
        : this(id, loggerFactory, configuration, frSSCD, new fiskaltrust.Middleware.Storage.AzureTableStorage.AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueueFRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IFRSSCD frSSCD, IStorageProvider storageProvider, IArchiveSigner? archiveSigner = null)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var logger = loggerFactory.CreateLogger<QueueFRBootstrapper>();
        // Sandbox and production queues sign with a real FR SCU; the in-memory one is for local runs (market-fr#221).
        frSSCD = new FRInMemoryScuGuard(frSSCD, FRInMemoryScuGuard.IsLocalDevelopment(configuration, middlewareConfiguration.IsSandbox), logger);
        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueueFRAsync(id)
            ?? throw new InvalidOperationException($"The French queue {id} has no ftQueueFR row. It is part of the queue's configuration (init_ftQueueFR).")).CashBoxIdentification);

        var chains = new FRChainStateProvider(storageProvider.CreateConfigurationRepository(), id);
        // The chain state is stored only once the signed queue item is (FRQueueStorageProvider).
        var queueStorageProvider = new FRQueueStorageProvider(new QueueStorageProvider(id, storageProvider), chains, storageProvider.CreateMiddlewareQueueItemRepository(), storageProvider.CreateMiddlewareActionJournalRepository());
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();
        var pipeline = new FRSigningPipeline(frSSCD, chains, queueItemRepository, storageProvider.CreateMiddlewareActionJournalRepository());

        var validationConfig = ForcedErrorLevel(ValidationConfiguration.FromConfiguration(configuration), logger);

        var receiptProcessorFR = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new ReceiptValidatorFR(new ReceiptReferenceProvider(queueItemRepository), async () => await frSSCD.GetInfoAsync().ConfigureAwait(false)),
            new LifecycleCommandProcessorFR(frSSCD, pipeline, queueStorageProvider, chains, queueItemRepository, new FRMigration(pipeline, chains, queueItemRepository)),
            new ReceiptCommandProcessorFR(pipeline),
            new DailyOperationsCommandProcessorFR(pipeline),
            new InvoiceCommandProcessorFR(pipeline),
            new ProtocolCommandProcessorFR(pipeline, storageProvider.CreateJournalFRCopyPayloadRepository(), queueItemRepository,
                new FRArchiveReceipt(pipeline, chains, queueItemRepository, storageProvider.CreateMiddlewareReceiptJournalRepository(), storageProvider.CreateMiddlewareActionJournalRepository())),
            validationConfig);

        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, FRMigrationGate.Wrap(chains, receiptProcessorFR.ProcessAsync), cashBoxIdentification, middlewareConfiguration);
        // A locally exported archive is signed by the queue's SCU (ADR 0007 amendment); archiveSigner overrides it.
        Func<Task<IArchiveSigner>> createArchiveSigner = archiveSigner is null
            ? async () => await ScuArchiveSigner.CreateAsync(frSSCD).ConfigureAwait(false)
            : () => Task.FromResult(archiveSigner);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorFR(id, storageProvider, createArchiveSigner, scuInfo: async () => await frSSCD.GetInfoAsync().ConfigureAwait(false), validationLevel: validationConfig.ValidationLevel.ToString(),chains: chains, sandbox: middlewareConfiguration.IsSandbox), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(new FRSignProcessor(signProcessor, chains), journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    /// <summary>
    /// FR always validates at <c>Error</c> (market-fr#221, fiscal-perimeter decision 24b): the FR rules refuse rather
    /// than log, so no receipt reaches signing unvalidated. A configured lower level is ignored and said so once, at
    /// start-up.
    /// </summary>
    internal static ValidationConfiguration ForcedErrorLevel(ValidationConfiguration configured, ILogger logger)
    {
        if (configured.ValidationLevel is { } level && level != ValidationLevel.Error)
        {
            logger.LogWarning("The configured ValidationLevel {ConfiguredLevel} is ignored: a French queue always validates at Error.", level);
        }
        return new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = configured.ValidationsInSignatures };
    }

    public Func<string, Task<string>> RegisterForSign() => _queue.RegisterForSign();

    public Func<string, Task<string>> RegisterForEcho() => _queue.RegisterForEcho();

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal() => _queue.RegisterForJournal();
}
