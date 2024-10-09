﻿using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueuePT;

public class QueuePTBootstrapper : IV2QueueBootstrapper
{
    public required Guid Id { get; set; }
    public required Dictionary<string, object> Configuration { get; set; }

    private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");

    public Queue RegisterForSign(ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = new MiddlewareConfiguration
        {
            CashBoxId = GetQueueCashbox(Id, Configuration),
            QueueId = Id,
            IsSandbox = Configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
            ServiceFolder = Configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
            Configuration = Configuration
        };

        var queuePT = new ftQueuePT();
        var signaturCreationUnitPT = new ftSignaturCreationUnitPT();
        var ptSSCD = new InMemorySCU(signaturCreationUnitPT);
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), storageProvider.GetConfigurationRepository(), new LifecyclCommandProcessorPT(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(), new ProtocolCommandProcessorPT(), queuePT.CashBoxIdentification);
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), storageProvider, signProcessorPT.ProcessAsync, queuePT.CashBoxIdentification, middlewareConfiguration);
        return new Queue(signProcessor, loggerFactory)
        {
            Id = Id,
            Configuration = Configuration,
        };
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
