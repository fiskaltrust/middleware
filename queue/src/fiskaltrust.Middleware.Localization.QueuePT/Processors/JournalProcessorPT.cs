using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.PT;
using fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class JournalProcessorPT : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;

    public JournalProcessorPT(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        var masterData = new AccountMasterData
        {
            AccountId = Guid.NewGuid(),
            AccountName = "fiskaltrust ",
            Street = "TEST STRET",
            Zip = "1111-2222",
            City = "Test",
            Country = "PT",
            TaxId = "199999999"
        };

        List<ftQueueItem> queueItems;
        if (request.From > 0)
        {
            queueItems = _storageProvider.GetMiddlewareQueueItemRepository().GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable().ToList();
        }
        else
        {
            queueItems = (await _storageProvider.GetMiddlewareQueueItemRepository().GetAsync()).ToList();
        }
        var data = SAFTMapping.CreateAuditFile(masterData, queueItems);
        using var memoryStream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(AuditFile));
        serializer.Serialize(memoryStream, data);
        memoryStream.Position = 0;
        yield return new JournalResponse
        {
            Chunk = memoryStream.ToArray().ToList()
        };
    }
}
