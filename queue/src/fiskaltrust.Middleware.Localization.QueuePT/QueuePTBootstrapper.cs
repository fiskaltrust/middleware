using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    public IPOS CreateQueuePT(MiddlewareConfiguration middlewareConfiguration, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        var ptSSCD = new InMemorySCU(new InMemorySCUConfiguration());
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorPT(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, new v2.v2.ftQueuePT { }), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(), new ProtocolCommandProcessorPT());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), storageProvider, signProcessorPT.ProcessAsync, null, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, null, null);
        return new Queue(signProcessor, journalProcessor, middlewareConfiguration);
    }
}
