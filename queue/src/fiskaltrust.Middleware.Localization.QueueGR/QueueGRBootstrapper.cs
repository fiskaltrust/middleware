using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.GR;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR;

#pragma warning disable
public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    private Queue _queue;

    public required Guid Id { get; set; }
    public required Dictionary<string, object> Configuration { get; set; }

    private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

    public QueueGRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = new MiddlewareConfiguration
        {
            CashBoxId = GetQueueCashbox(id, configuration),
            QueueId = id,
            IsSandbox = configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
            ServiceFolder = configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
            Configuration = configuration
        };

        var queueGR = JsonConvert.DeserializeObject<List<ftQueueGR>>(Configuration["init_ftQueueGR"].ToString()).First();
        var storageProvider = new AzureStorageProvider(loggerFactory, Id, Configuration);
        var signaturCreationUnitPT = new ftSignaturCreationUnitGR();
        var ptSSCD = new MyDataApiClient(Configuration["aade-user-id"].ToString(), Configuration["ocp-apim-subscription-key"].ToString());
        var queueStorageProvider = new QueueStorageProvider(Id, storageProvider, storageProvider.GetConfigurationRepository(), storageProvider.GetMiddlewareQueueItemRepository(), storageProvider.GetMiddlewareReceiptJournalRepository(), storageProvider.GetMiddlewareActionJournalRepository());
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorGR(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorGR(ptSSCD, queueGR, signaturCreationUnitPT), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(), new ProtocolCommandProcessorGR());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, queueGR.CashBoxIdentification, middlewareConfiguration);
        _queue = new Queue(signProcessor, loggerFactory)
        {
            Id = Id,
            Configuration = Configuration,
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

    private static Guid GetQueueCashbox(Guid queueId, Dictionary<string, object> configuration)
    {
        var key = "init_ftQueue";
        if (configuration.ContainsKey(key))
        {
            var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key]!.ToString()!);
            return queues.Where(q => q.ftQueueId == queueId).First().ftCashBoxId;
        }
        else
        {
            throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
        }
    }
}
