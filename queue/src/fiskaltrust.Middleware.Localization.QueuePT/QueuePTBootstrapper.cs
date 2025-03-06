using System.Security.Cryptography;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.QueuePT.PTSSCD;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.PT;
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
        signaturCreationUnitPT.PrivateKey = "-----BEGIN PRIVATE KEY-----\r\nMIICeAIBADANBgkqhkiG9w0BAQEFAASCAmIwggJeAgEAAoGBAOPt6HzaExZ03nfx\r\njBBkJ5iZnoNQb/UpUtmrK05fQiyE0PfYK/K6stEMnLJ4gjetzRg7CE0pwkO8vfR7\r\n6xjOBVlBLImVCW8PS91gEuTKI9CGJtQzn53FBlqjWXDqE1CggHUmlIet8hkFJEm3\r\nQwH6/sXPF0IT6OHSe3GIDlXLIGd3AgMBAAECgYAGNwd+2AdrNlaWmKyECecWfyHW\r\nXMwguDa9HrC4m1pXkmuMRoW0Qaj8kEZ5i1WppQCRp5JrYDce17eqQfLAI2X74uFP\r\nSLZmzpkT6heMJgBchI01mZMjO3GF5j3KLaBuMnY856GE/M1tkfgjBg6HN9AkJQ5v\r\nCQSFTdkDZddtsCq4sQJBAPU56DOBW1DCaI+DXnrdelZND0JkxLTYNBOssjm3Z4du\r\nmXSHTODBdYPEEnnHj1J32MrN+wh+qr5FnN+HFiB20q8CQQDt8XX4kb6Bq1W0IS6z\r\ne0Rvfuw5wWTMdHDNUdwj4U7TYnWSeXF6wf2zRW2BvJcj0drwJbyYA3QyZFqf0IU0\r\nRgm5AkEAx1/MNMvwFSnqXvv8vcIB69Z9GIrbDvlU5cYbpSdDCe5W31H9pCJFy9qG\r\n9vHTycXcwY5UkeSCJ25ri6TFzaEtywJBAJ6VgrnTcTP9HFa8kuKecmMZJZnssiCu\r\nLow5Vc44GRA7m/6uoBpf9pWn3S9NoTIHaLMLg6GRE72OMvQ2xsCrOUkCQQCEZ32z\r\nRjkfjWTiMvgg2uoJmlqKS8DVo8wwi6YLKpjMn/IKJxS+c8HItliM7qcgLSJf1tMA\r\n6cXu8ErRRiMeSvX3\r\n-----END PRIVATE KEY-----\r\n"; 
        var ptSSCD = new InMemorySCU(signaturCreationUnitPT);
        var queueStorageProvider = new QueueStorageProvider(id, storageProvider);
        var signProcessorPT = new ReceiptProcessor(loggerFactory.CreateLogger<ReceiptProcessor>(), new LifecycleCommandProcessorPT(storageProvider.GetConfigurationRepository()), new ReceiptCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT, storageProvider.GetMiddlewareQueueItemRepository()), new DailyOperationsCommandProcessorPT(), new InvoiceCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT, storageProvider.GetMiddlewareQueueItemRepository()), new ProtocolCommandProcessorPT(ptSSCD, queuePT, signaturCreationUnitPT, storageProvider.GetMiddlewareQueueItemRepository()));
        var signProcessor = new SignProcessor(loggerFactory.CreateLogger<SignProcessor>(), queueStorageProvider, signProcessorPT.ProcessAsync, queuePT.CashBoxIdentification, middlewareConfiguration);
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
