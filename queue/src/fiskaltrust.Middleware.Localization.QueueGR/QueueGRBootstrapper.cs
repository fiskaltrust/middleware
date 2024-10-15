using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.GR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueueGRBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queueGR = JsonConvert.DeserializeObject<List<ftQueueGR>>(configuration["init_ftQueueGR"]!.ToString()!).First();
        var signaturCreationUnitPT = new ftSignaturCreationUnitGR();
        var ptSSCD = MyDataApiClient.CreateClient(configuration);
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecyclCommandProcessorGR(queueStorageProvider), new ReceiptCommandProcessorGR(ptSSCD, queueGR, signaturCreationUnitPT), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(), new ProtocolCommandProcessorGR());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, queueGR.CashBoxIdentification, middlewareConfiguration);
        _queue = new Queue(signProcessor, loggerFactory)
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
}
