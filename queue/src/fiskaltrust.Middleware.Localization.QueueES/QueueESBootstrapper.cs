using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.Middleware.Storage.AzureTableStorage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueES;


public class QueueESBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueESBootstrapper(Guid id, ILoggerFactory loggerFactory, IClientFactory<IESSSCD> clientFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);

        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var masterDataService = new MasterDataService(configuration, storageProvider);
        storageProvider.Initialized.Wait();
        var masterData = masterDataService.GetCurrentDataAsync().Result; // put this in an async scu init process
        var queueES = JsonConvert.DeserializeObject<List<ftQueueES>>(configuration["init_ftQueueES"]!.ToString()!).First();

        Console.WriteLine(configuration["init_ftQueueES"].ToString());
        if (!queueES.ftSignaturCreationUnitESId.HasValue)
        {
            throw new ArgumentException($"Configuration must contain 'SignaturCreationUnitESId' parameter.");
        }

        var signaturCreationUnitES = JsonConvert.DeserializeObject<List<ftSignaturCreationUnitES>>(configuration["init_ftSignaturCreationUnitES"]!.ToString()!).First(x => x.ftSignaturCreationUnitESId == queueES.ftSignaturCreationUnitESId.Value);
        Console.WriteLine(configuration["init_ftSignaturCreationUnitES"].ToString());

        var config = new ClientConfiguration
        {
            Url = signaturCreationUnitES.Url.ToString(),
        };

        var queueESConfiguration = QueueESConfiguration.FromMiddlewareConfiguration(middlewareConfiguration);

        if (queueESConfiguration.ScuTimeoutMs.HasValue)
        {
            config.Timeout = TimeSpan.FromMilliseconds(queueESConfiguration.ScuTimeoutMs.Value);
        }
        if (queueESConfiguration.ScuMaxRetries.HasValue)
        {
            config.RetryCount = queueESConfiguration.ScuMaxRetries.Value;
        }
        var esSSCD = clientFactory.CreateClient(config);


        var signProcessorES = new ReceiptProcessor(
            loggerFactory.CreateLogger<ReceiptProcessor>(),
            new LifecycleCommandProcessorES(
                queueStorageProvider
            ),
            new ReceiptCommandProcessorES(
                esSSCD,
                storageProvider.GetConfigurationRepository(),
                storageProvider.GetMiddlewareQueueItemRepository()
            ),
            new DailyOperationsCommandProcessorES(
                esSSCD,
                queueStorageProvider),
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
