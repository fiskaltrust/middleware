using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

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
        var queueItems = await _storageProvider.GetMiddlewareQueueItemRepository().GetAsync();
        var data = SAFTMapping.CreateAuditFile(queueItems.ToList());
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
