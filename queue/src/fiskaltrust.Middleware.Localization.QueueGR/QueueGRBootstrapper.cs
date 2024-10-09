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

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    public required Guid Id { get; set; }
    public required Dictionary<string, object> Configuration { get; set; }

    private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

    public Func<string, Task<string>> RegisterForSign(ILoggerFactory loggerFactory)
    {
        var middlewareConfiguration = new MiddlewareConfiguration
        {
            CashBoxId = GetQueueCashbox(Id, Configuration),
            QueueId = Id,
            IsSandbox = Configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
            ServiceFolder = Configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
            Configuration = Configuration
        };
        var storageProvider = new AzureStorageProvider(loggerFactory, Id, Configuration);
        var queueGR = new ftQueueGR();
        var signaturCreationUnitPT = new ftSignaturCreationUnitGR();
        var ptSSCD = new MyDataApiClient("", "");
        var queueStorageProvider = new QueueStorageProvider(Id, storageProvider.GetConfigurationRepository(), storageProvider.GetMiddlewareQueueItemRepository(), storageProvider.GetMiddlewareReceiptJournalRepository(), storageProvider.GetMiddlewareActionJournalRepository());
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorGR(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorGR(ptSSCD, queueGR, signaturCreationUnitPT), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(), new ProtocolCommandProcessorGR());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, queueGR.CashBoxIdentification, middlewareConfiguration);
        return new Queue(signProcessor, loggerFactory)
        {
            Id = Id,
            Configuration = Configuration,
        }.RegisterForSign();
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
