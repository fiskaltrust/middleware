using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

#pragma warning disable
public class JournalProcessorGR : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;
    private readonly MasterDataConfiguration _masterDataConfiguration;

    public JournalProcessorGR(IStorageProvider storageProvider, MasterDataConfiguration masterDataConfiguration)
    {
        _storageProvider = storageProvider;
        _masterDataConfiguration = masterDataConfiguration;
    }

    public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        var queueItems = new List<ftQueueItem>();
        if (request.From > 0)
        {
            queueItems = (_storageProvider.GetMiddlewareQueueItemRepository().GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable()).ToList();
        }
        else
        {
            queueItems = (await _storageProvider.GetMiddlewareQueueItemRepository().GetAsync()).ToList();
        }

        var aadFactory = new AADEFactory(_masterDataConfiguration);
        using var memoryStream = new MemoryStream();
        var invoiecDoc = aadFactory.MapToInvoicesDoc(queueItems.ToList());
        if(request.To == -1)
        {
            invoiecDoc.invoice = invoiecDoc.invoice.OrderByDescending(x => x.mark).Take(1).ToArray();
        }
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        xmlSerializer.Serialize(memoryStream, invoiecDoc);
        memoryStream.Position = 0;
        yield return new JournalResponse
        {
            Chunk = memoryStream.ToArray().ToList()
        };
    }
}
