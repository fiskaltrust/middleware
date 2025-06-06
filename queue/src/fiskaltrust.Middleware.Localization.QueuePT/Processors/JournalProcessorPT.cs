using System.Text;
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
            queueItems = _storageProvider.GetMiddlewareQueueItemRepository().GetEntriesOnOrAfterTimeStampAsync(request.From).ToBlockingEnumerable().ToList();
        }
        else
        {
            queueItems = (await _storageProvider.GetMiddlewareQueueItemRepository().GetAsync()).ToList();
        }
        var data = new SaftExporter().SerializeAuditFile(masterData, queueItems, (int) request.To);
        yield return new JournalResponse
        {
            Chunk = Encoding.UTF8.GetBytes(data).ToArray().ToList()
        };
    }
}
