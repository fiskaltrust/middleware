using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueueEU.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueEU;


public class QueueEUBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueEUBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);

        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        storageProvider.Initialized.Wait();

        var configurationRepository = storageProvider.GetConfigurationRepository();
        var queueEU = configurationRepository.GetQueueEUAsync(id).Result;
        if (queueEU is null)
        {
            queueEU = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueueEU>>(configuration["init_ftQueueEU"]!.ToString()!).First();
            configurationRepository.InsertOrUpdateQueueEUAsync(queueEU);
        }

        var signProcessorEU = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new LifecycleCommandProcessorEU(
                queueStorageProvider
            ),
            new ReceiptCommandProcessorEU(),
            new DailyOperationsCommandProcessorEU(),
            new InvoiceCommandProcessorEU(),
            new ProtocolCommandProcessorEU()
        );
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorEU.ProcessAsync, queueEU.CashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorEU(), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
