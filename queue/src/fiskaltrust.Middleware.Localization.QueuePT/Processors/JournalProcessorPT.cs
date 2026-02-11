using System.Net.Mime;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class JournalProcessorPT : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;

    public JournalProcessorPT(IStorageProvider storageProvider)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _storageProvider = storageProvider;
    }

    public (ContentType contentType, IAsyncEnumerable<byte[]> result) ProcessAsync(JournalRequest request)
    {
        return (new ContentType(MediaTypeNames.Application.Xml) { CharSet = Encoding.GetEncoding("windows-1252").WebName }, ProcessSAFTAsync(request));
    }

    public async IAsyncEnumerable<byte[]> ProcessSAFTAsync(JournalRequest request)
    {
        var masterData = new AccountMasterData
        {
            AccountId = Guid.NewGuid(),
            AccountName = "FISKALTRUST CONSULTING GMBH - SUCURSAL EM",
            Street = "AV DA REPUBLICA N 35 4 ANDAR",
            Zip = "1050-189",
            City = "Lisboa",
            Country = "PT",
            TaxId = "980833310"
        };

        List<ftQueueItem> queueItems;
        if (request.From > 0)
        {
            queueItems = (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable().ToList();
        }
        else
        {
            queueItems = (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetAsync()).ToList();
        }
        var documentStatusProvider = new DocumentStatusProvider(_storageProvider.CreateMiddlewareQueueItemRepository());
        var data = new SaftExporter(documentStatusProvider).SerializeAuditFile(masterData, queueItems, (int) request.To);
        yield return data;
    }
}
