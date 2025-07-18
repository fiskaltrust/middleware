using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.SCU.GR.MyData;
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

    public async Task<(ContentType, PipeReader)> ProcessAsync(JournalRequest request)
    {
        var queueItems = new List<ftQueueItem>();
        if (request.From.HasValue && request.From.Value.Ticks > 0)
        {
            queueItems = ((await _storageProvider.CreateMiddlewareQueueItemRepository()).GetEntriesOnOrAfterTimeStampAsync(request.From.Value.Ticks).ToBlockingEnumerable()).ToList();
        }
        else
        {
            queueItems = (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetAsync()).ToList();
        }

        var aadFactory = new AADEFactory(_masterDataConfiguration);
        using var memoryStream = new MemoryStream();
        var invoiceDoc = aadFactory.MapToInvoicesDoc(queueItems.ToList());
        if (request.Take.HasValue)
        {
            invoiceDoc.invoice = invoiceDoc.invoice.OrderBy(x => x.mark * Math.Sign(request.Take.Value)).Take((int) request.Take.Value).ToArray();
        }
        var xmlSerializer = new XmlSerializer(typeof(InvoicesDoc));
        xmlSerializer.Serialize(memoryStream, invoiceDoc);
        memoryStream.Position = 0;

        return (new ContentType(MediaTypeNames.Application.Xml), PipeReader.Create(memoryStream));
    }
}
