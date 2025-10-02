using System.IO.Pipelines;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.Json;
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

    public QueuePTBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IPTSSCD ptSSCD) : this(id, loggerFactory, configuration, ptSSCD, new AzureStorageProvider(loggerFactory, id, configuration)) { }    

    public QueuePTBootstrapper(Guid id, ILoggerFactory loggerFactory, Dictionary<string, object> configuration, IPTSSCD ptSSCD, IStorageProvider storageProvider)
    {
        var middlewareConfiguration = MiddlewareConfigurationFactory.CreateMiddlewareConfiguration(id, configuration);
        var queuePT = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ftQueuePT>>(configuration["init_ftQueuePT"]!.ToString()!).First();
        queuePT.IssuerTIN = "980833310";
        queuePT.NumeratorStorage = JsonSerializer.Deserialize<NumeratorStorage>("""
            {
              "InvoiceSeries": {
                "TypeCode": "FT",
                "ATCUD": "AAJFJGVC33",
                "Series": "ft2025019d"
              },
              "SimplifiedInvoiceSeries": {
                "TypeCode": "FS",
                "ATCUD": "AAJFJ4VC3W",
                "Series": "ft2025019d"
              },
              "CreditNoteSeries": {
                "TypeCode": "NC",
                "ATCUD": "AAJFJ3VC34",
                "Series": "ft2025019d"
              },
              "HandWrittenFSSeries": {
                "TypeCode": "FS",
                "ATCUD": "AAJFJBKFZR",
                "Series": "ft2025771b"
              },
              "ProFormaSeries": {
                "TypeCode": "PF",
                "ATCUD": "AAJFJ9VC37",
                "Series": "ft2025019d"
              },
              "PaymentSeries": {
                "TypeCode": "RG",
                "ATCUD": "AAJFJMVC3G",
                "Series": "ft2025019d"
              },
              "BudgetSeries": {
                "TypeCode": "OR",
                "ATCUD": "AAJFJKVC3P",
                "Series": "ft2025eb51"
              },
              "TableChecqueSeries": {
                "TypeCode": "CM",
                "ATCUD": "AAJFJ2VC3R",
                "Series": "ft2025eb51"
              }
            }
            """);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorPT(queueStorageProvider), new ReceiptCommandProcessorPT(ptSSCD, queuePT, storageProvider.CreateMiddlewareQueueItemRepository()), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(ptSSCD, queuePT, storageProvider.CreateMiddlewareQueueItemRepository()), new ProtocolCommandProcessorPT(ptSSCD, queuePT, storageProvider.CreateMiddlewareQueueItemRepository()));
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

    public Func<string, Task<(ContentType contentType, PipeReader reader)>> RegisterForJournal()
    {
        return _queue.RegisterForJournal();
    }
}
