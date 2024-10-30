using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;


namespace fiskaltrust.Middleware.Localization.QueueES;


public class QueueESBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueESBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueES = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueueES>>(configuration["init_ftQueueES"]!.ToString()!).First();

        var signaturCreationUnitES = new ftSignaturCreationUnitES();
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var masterDataService = new MasterDataService(configuration, storageProvider);
        var masterData = masterDataService.GetCurrentDataAsync().Result; // put this in an async scu init process
        var esSSCD = new InMemorySCU(signaturCreationUnitES, masterData);
        var signProcessorES = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new LifecycleCommandProcessorES(
                esSSCD,
                queueStorageProvider
            ),
            new ReceiptCommandProcessorES(
                esSSCD,
                queueES,
                signaturCreationUnitES,
                queueStorageProvider
            ),
            new DailyOperationsCommandProcessorES(),
            new InvoiceCommandProcessorES(),
            new ProtocolCommandProcessorES()
        );
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorES.ProcessAsync, queueES.CashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorES(storageProvider.GetMiddlewareReceiptJournalRepository(), storageProvider.GetMiddlewareQueueItemRepository(), masterData), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
