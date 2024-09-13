using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    public IPOS CreateQueuePT(
        MiddlewareConfiguration middlewareConfiguration, 
        ILoggerFactory loggerFactory, 
        IConfigurationRepository configurationRepository,
        IMiddlewareQueueItemRepository middlewareQueueItemRepository,
        IMiddlewareReceiptJournalRepository middlewareReceiptJournalRepository,
        IMiddlewareActionJournalRepository middlewareActionJournalRepository,
        ICryptoHelper cryptoHelper)
    {
        var ptSSCD = new InMemorySCU(new InMemorySCUConfiguration());
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), configurationRepository, new LifecyclCommandProcessorPT(configurationRepository), new ReceiptCommandProcessorPT(ptSSCD), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(), new ProtocolCommandProcessorPT());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), configurationRepository, middlewareQueueItemRepository, middlewareReceiptJournalRepository, middlewareActionJournalRepository, cryptoHelper, signProcessorPT.ProcessAsync, null, middlewareConfiguration);
        var journalProcessor = new JournalProcessor(configurationRepository, middlewareQueueItemRepository, middlewareReceiptJournalRepository, middlewareActionJournalRepository, null, null, null, null, null, null);
        return new Queue(signProcessor, journalProcessor, middlewareConfiguration);
    }
}
