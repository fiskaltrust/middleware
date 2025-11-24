using System.IO.Pipelines;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.Json;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.ifPOS.v2.pt;
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
    if (queuePT.IssuerTIN is null)
    {
      queuePT.IssuerTIN = "980833310";
    }

    if (configuration.ContainsKey("NumeratorStorage"))
    {
      queuePT.NumeratorStorage = JsonSerializer.Deserialize<NumeratorStorage>(configuration["NumeratorStorage"]!.ToString()!);
    }
    else
    {
      queuePT.NumeratorStorage = JsonSerializer.Deserialize<NumeratorStorage>("""
            {
              "InvoiceSeries": {
                "TypeCode": "FT",
                "ATCUD": "AAJFJ2K6JF",
                "Series": "ft2025b814"
              },
              "SimplifiedInvoiceSeries": {
                "TypeCode": "FS",
                "ATCUD": "AAJFJNK6JJ",
                "Series": "ft20257d14"
              },
              "CreditNoteSeries": {
                "TypeCode": "NC",
                "ATCUD": "AAJFJ6K6J5",
                "Series": "ft2025128b"
              },
              "HandWrittenFSSeries": {
                "TypeCode": "FS",
                "ATCUD": "AAJFJHK6J6",
                "Series": "ft20250a62"
              },
              "ProFormaSeries": {
                "TypeCode": "PF",
                "ATCUD": "AAJFJFK6JH",
                "Series": "ft20253a3b"
              },
              "PaymentSeries": {
                "TypeCode": "RG",
                "ATCUD": "AAJFJ8K6JT",
                "Series": "ft2025a4fa"
              },
              "BudgetSeries": {
                "TypeCode": "OR",
                "ATCUD": "AAJFJYK6JN",
                "Series": "ft20255389"
              },
              "TableChecqueSeries": {
                "TypeCode": "CM",
                "ATCUD": "AAJFJPK6JZ",
                "Series": "ft20259c2f"
              }
            }
            """);
    }
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
