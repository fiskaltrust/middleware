using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Abstractions;
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

    public QueuePLBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IClientFactory<IPLSSCD> clientFactory)
        : this(id, loggerFactory, configuration, clientFactory, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueuePLBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IClientFactory<IPLSSCD> clientFactory, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueuePLAsync(id)).CashBoxIdentification);

        // The SCU is resolved from the configuration repository — not from the init config — so the
        // repository rows (ftQueuePL → ftSignaturCreationUnitPL) are the single source of truth for
        // which register the queue talks to, the same way QueueES resolves its SCU.
        var plSSCD = new AsyncLazy<IPLSSCD>(async () =>
        {
            var configurationRepository = await storageProvider.CreateConfigurationRepository();
            var queue = await configurationRepository.GetQueuePLAsync(id);
            if (queue.ftSignaturCreationUnitPLId is not { } signaturCreationUnitPLId)
            {
                throw new InvalidOperationException($"The queue {id} has no ftSignaturCreationUnitPLId configured. A PL queue needs a signature creation unit to communicate with the fiscal register.");
            }
            var scu = await configurationRepository.GetSignaturCreationUnitPLAsync(signaturCreationUnitPLId)
                ?? throw new InvalidOperationException($"The signature creation unit {signaturCreationUnitPLId} configured for queue {id} was not found in the configuration repository.");
            return clientFactory.CreateClient(new ClientConfiguration
            {
                Timeout = TimeSpan.FromSeconds(15),
                Url = scu.Url
            });
        });

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var queueItemRepository = storageProvider.CreateMiddlewareQueueItemRepository();

        // PL launches with enforcing validation: there are no legacy integrations to stay
        // compatible with, and the queue currency rule (PLN, rfcs/0705-queue-single-currency)
        // must reject rather than log. A configured ValidationLevel still takes precedence.
        var validationConfig = ValidationConfiguration.FromConfiguration(configuration);
        if (validationConfig.ValidationLevel is null)
        {
            validationConfig = new ValidationConfiguration { ValidationLevel = ValidationLevel.Error, ValidationsInSignatures = validationConfig.ValidationsInSignatures };
        }

        var signProcessorPL = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new Validation.ReceiptValidatorPL(new ReceiptReferenceProvider(queueItemRepository)),
            new LifecycleCommandProcessorPL(plSSCD, queueStorageProvider),
            new ReceiptCommandProcessorPL(plSSCD),
            new DailyOperationsCommandProcessorPL(plSSCD),
            new InvoiceCommandProcessorPL(),
            new ProtocolCommandProcessorPL(),
            validationConfig);
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
