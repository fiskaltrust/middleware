﻿using System.Security.Cryptography;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    public QueuePTBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queuePT = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueuePT>>(configuration["init_ftQueuePT"]!.ToString()!).First();
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var signaturCreationUnitPT = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftSignaturCreationUnitPT>>(configuration["init_ftSignaturCreationUnitPT"]!.ToString()!).First();
        //signaturCreationUnitPT.PrivateKey = ""; 
        var ptSSCD = new InMemorySCU(signaturCreationUnitPT);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);

        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorPT(storageProvider.CreateConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, queuePT, storageProvider.CreateMiddlewareQueueItemRepository()), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT, storageProvider.CreateMiddlewareQueueItemRepository()), new ProtocolCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT, storageProvider.CreateMiddlewareQueueItemRepository()));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, new(() => Task.FromResult(queuePT.CashBoxIdentification)), middlewareConfiguration);
        var journalProcessor = new JournalProcessor(storageProvider, new JournalProcessorPT(storageProvider), configuration, loggerFactory.CreateLogger<JournalProcessor>());
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
