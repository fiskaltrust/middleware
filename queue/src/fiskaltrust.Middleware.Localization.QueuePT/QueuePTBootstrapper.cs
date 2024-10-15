﻿using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.PT;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    private readonly Queue _queue;

    private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

    public QueuePTBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queuePT = JsonConvert.DeserializeObject<List<ftQueuePT>>(configuration["init_ftQueuePT"]!.ToString()!).First();
        var storageProvider = new AzureStorageProvider(loggerFactory, id, configuration);
        var signaturCreationUnitPT = new ftSignaturCreationUnitPT();
        var ptSSCD = new InMemorySCU(signaturCreationUnitPT);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorPT(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(), new ProtocolCommandProcessorPT());
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, queuePT.CashBoxIdentification, middlewareConfiguration);
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