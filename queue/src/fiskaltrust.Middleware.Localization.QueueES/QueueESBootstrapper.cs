using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.QueueES.ValidationFV;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2.Validation;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES;


public class QueueESBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueESBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IESSSCD> clientFactory, Dictionary<string, object> configuration)
        : this(id, loggerFactory, clientFactory, configuration, new AzureStorageProvider(loggerFactory, id, configuration)) { }

    public QueueESBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IESSSCD> clientFactory, Dictionary<string, object> configuration, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueESConfiguration = QueueESConfiguration.FromMiddlewareConfiguration(middlewareConfiguration);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var masterDataService = new MasterDataService(configuration, storageProvider);

        var cashBoxIdentification = new AsyncLazy<string>(async () => (await (await storageProvider.CreateConfigurationRepository()).GetQueueESAsync(id)).CashBoxIdentification);

        var essscd = new AsyncLazy<IESSSCD>(async () =>
        {
            var configurationRepository = await storageProvider.CreateConfigurationRepository();
            var queue = await configurationRepository.GetQueueESAsync(id);
            var scu = await configurationRepository.GetSignaturCreationUnitESAsync(queue.ftSignaturCreationUnitESId);
            return clientFactory.CreateClient(new ClientConfiguration
            {
                Timeout = queueESConfiguration.ScuTimeoutMs.HasValue ? TimeSpan.FromMilliseconds(queueESConfiguration.ScuTimeoutMs.Value) : TimeSpan.FromSeconds(15),
                RetryCount = queueESConfiguration.ScuMaxRetries.HasValue ? queueESConfiguration.ScuMaxRetries.Value : null,
                Url = scu.Url
            });
        });

        var repo = storageProvider.CreateMiddlewareQueueItemRepository();
        var fvValidator = new ReceiptValidator(new FVReceiptReferenceProvider(repo), new Validation.VoidValidator(repo));
        var shadowLogger = loggerFactory.CreateLogger("ShadowValidation.ES");

        var receiptProcessor = new ReceiptCommandProcessorES(
            loggerFactory.CreateLogger<ReceiptCommandProcessorES>(),
            essscd,
            storageProvider.CreateConfigurationRepository(),
            repo,
            storageProvider.CreateMiddlewareJournalESRepository()
        );
        var invoiceProcessor = new InvoiceCommandProcessorES(
            loggerFactory.CreateLogger<InvoiceCommandProcessorES>(),
            essscd,
            storageProvider.CreateConfigurationRepository(),
            repo,
            storageProvider.CreateMiddlewareJournalESRepository()
        );

        receiptProcessor.SetShadowValidation(fvValidator, shadowLogger);
        invoiceProcessor.SetShadowValidation(fvValidator, shadowLogger);

        var signProcessorES = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), fvValidator, new LifecycleCommandProcessorES(queueStorageProvider, storageProvider.CreateConfigurationRepository()), receiptProcessor, new DailyOperationsCommandProcessorES(essscd, queueStorageProvider), invoiceProcessor, new ProtocolCommandProcessorES());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorES.ProcessAsync, cashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorES(storageProvider.CreateMiddlewareJournalESRepository()), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
