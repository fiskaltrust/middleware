using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.myDataSCU;
using fiskaltrust.Middleware.Localization.QueueGR.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.GR;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRBootstrapper : IV2QueueBootstrapper
{
    public IPOS CreateQueueGR(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        var queueGR = new ftQueueGR();
        var signaturCreationUnitPT = new ftSignaturCreationUnitGR();
        var ptSSCD = new MyDataApiClient("", "");
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorGR(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorGR(ptSSCD, queueGR, signaturCreationUnitPT), new DailyOperationsCommandProcessorGR(), new InvoiceCommandProcessorGR(), new ProtocolCommandProcessorGR(), queueGR.CashBoxIdentification);
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), storageProvider, signProcessorPT.ProcessAsync, null, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, null, null);
        return new Queue(signProcessor, journalProcessor, middlewareConfiguration);
    }
}
