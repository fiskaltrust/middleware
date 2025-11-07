using System.IO.Pipelines;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

#pragma warning disable
public class JournalProcessorBE : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;
    private readonly MasterDataConfiguration _masterDataConfiguration;

    public JournalProcessorBE(IStorageProvider storageProvider, MasterDataConfiguration masterDataConfiguration)
    {
        _storageProvider = storageProvider;
        _masterDataConfiguration = masterDataConfiguration;
    }

    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
    {
        return (new ContentType(MediaTypeNames.Application.Json), ProcessBEJournalAsync(request));
    }

    public async IAsyncEnumerable<byte[]> ProcessBEJournalAsync(JournalRequest request)
    {
        var queueItems = new List<ftQueueItem>();
        if (request.From > 0)
        {
            queueItems = ((await _storageProvider.CreateMiddlewareQueueItemRepository()).GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable()).ToList();
        }
        else
        {
            queueItems = (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetAsync()).ToList();
        }

        // For Belgium, we'll return a simple JSON export instead of XML like Greece
        var journalData = new
        {
            exportedAt = DateTime.UtcNow,
            market = "BE",
            queueItems = queueItems.Select(qi => new
            {
                qi.ftQueueItemId,
                qi.ftQueueId,
                qi.ftQueueRow,
                qi.cbTerminalID,
                qi.cbReceiptReference,
                qi.cbReceiptMoment,
                qi.ftWorkMoment,
                qi.request,
                qi.response
            }).ToList()
        };

        using var memoryStream = new MemoryStream();
        var json = System.Text.Json.JsonSerializer.Serialize(journalData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        yield return bytes;
    }
}