using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueGRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IGRSSCD grSSCD)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueGR = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueueGR>>(configuration["init_ftQueueGR"]!.ToString()!).First();
        var signaturCreationUnitGR = new ftSignaturCreationUnitGR();

        //var storageProvider = new AzureStorageProvider(loggerFactory, id, Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonSerializer.Serialize(configuration)));
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var signProcessorGR = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorGR(queueStorageProvider), new ReceiptCommandProcessorGR(grSSCD, queueGR, signaturCreationUnitGR, storageProvider.GetMiddlewareQueueItemRepository()), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(grSSCD, queueGR, signaturCreationUnitGR, storageProvider.GetMiddlewareQueueItemRepository()), new ProtocolCommandProcessorGR(grSSCD, queueGR, signaturCreationUnitGR, storageProvider.GetMiddlewareQueueItemRepository()));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorGR.ProcessAsync, queueGR.CashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorGR(storageProvider, GetFromConfig(configuration) ?? new MasterDataConfiguration { }), configuration, loggerFactory.CreateLogger<JournalProcessor>());
        _queue = new Queue(signProcessor, journalProcessor, loggerFactory)
        {
            Id = id,
            Configuration = configuration,
        };
    }

    public MasterDataConfiguration? GetFromConfig(Dictionary<string, object> configuration)
    {
        return configuration.ContainsKey("init_masterData") ? JsonConvert.DeserializeObject<MasterDataConfiguration>(configuration["init_masterData"].ToString()!) : null;
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
