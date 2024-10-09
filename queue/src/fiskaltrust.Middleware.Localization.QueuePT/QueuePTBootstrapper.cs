using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.PT;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    public Queue CreateQueueGR(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        var queuePT = new ftQueuePT();
        var signaturCreationUnitPT = new ftSignaturCreationUnitPT();
        var ptSSCD = new InMemorySCU(signaturCreationUnitPT);
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorPT(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(), new ProtocolCommandProcessorPT(), queuePT.CashBoxIdentification);
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), storageProvider, signProcessorPT.ProcessAsync, queuePT.CashBoxIdentification, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorPT(), loggerFactory.CreateLogger<JournalProcessor>());
        return new Queue(signProcessor, journalProcessor, middlewareConfiguration);
    }
}
