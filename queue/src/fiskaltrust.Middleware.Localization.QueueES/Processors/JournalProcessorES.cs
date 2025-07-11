using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Identity.Client;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class JournalProcessorES : IJournalProcessor
{
    private readonly AsyncLazy<IMiddlewareReceiptJournalRepository> _receiptJournalRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository;
    private readonly MasterDataService _masterDataService;

    public JournalProcessorES(AsyncLazy<IMiddlewareReceiptJournalRepository> receiptJournalRepository, AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository, MasterDataService masterDataService)
    {
        _receiptJournalRepository = receiptJournalRepository;
        _queueItemRepository = queueItemRepository;
        _masterDataService = masterDataService;
    }

    public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {

        var veriFactu = await new VeriFactuMapping(await _masterDataService.GetCurrentDataAsync(), await _queueItemRepository).CreateRegFactuSistemaFacturacionAsync((await _receiptJournalRepository).GetEntriesOnOrAfterTimeStampAsync(0).SelectAwait(async x => await (await _queueItemRepository).GetAsync(x.ftQueueItemId)));
        yield return new JournalResponse
        {
            Chunk = Encoding.UTF8.GetBytes(veriFactu.XmlSerialize()).ToList()
        };
    }
}
