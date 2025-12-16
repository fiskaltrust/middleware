using System.IO.Pipelines;
using System.Net.Mime;
using System.Text.Json;
using fiskaltrust.ifPOS.v2.be;
using fiskaltrust.Middleware.Localization.QueueBE.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueBE;

public class QueueBEBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueBEBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IBESSCD beSSCD)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var signaturCreationUnitBE = new ftSignaturCreationUnitBE();
        // With the storage project in the middleware repo this _could_ already be done correctly.
        var queueBE = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueueBE>>(configuration["init_ftQueueBE"]!.ToString()!).First();

        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);

        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var signProcessorBE = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorBE(queueStorageProvider), new ReceiptCommandProcessorBE(beSSCD, storageProvider.CreateMiddlewareQueueItemRepository()), new DailyOperationsCommandProcessorBE(), new InvoiceCommandProcessorBE(beSSCD, storageProvider.CreateMiddlewareQueueItemRepository()), new ProtocolCommandProcessorBE(beSSCD));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorBE.ProcessAsync, new(() => Task.FromResult(queueBE.CashBoxIdentification)), middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorBE(storageProvider, GetFromConfig(configuration) ?? new MasterDataConfiguration { }), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(signProcessor, journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    public MasterDataConfiguration? GetFromConfig(Dictionary<string, object> configuration)
    {
        // The masterdata should be already saved in the database
        // var masterDataService = new MasterDataService(configuration, storageProvider);
        return configuration.ContainsKey("init_masterData") ? JsonConvert.DeserializeObject<MasterDataConfiguration>(configuration["init_masterData"].ToString()!) : null;

        // Do we want to continue with the masterdata process like it is in germany?
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