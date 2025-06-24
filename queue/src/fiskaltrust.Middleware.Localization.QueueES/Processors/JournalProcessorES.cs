using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueuePT.Processors;

public class JournalProcessorES : IJournalProcessor
{
    private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;
    private readonly VeriFactuMapping _veriFactuMapping;

    public JournalProcessorES(IMiddlewareReceiptJournalRepository receiptJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, MasterDataConfiguration masterData)
    {
        _receiptJournalRepository = receiptJournalRepository;
        _queueItemRepository = queueItemRepository;
        _veriFactuMapping = new VeriFactuMapping(masterData);
    }

    public async IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        var veriFactu = await _veriFactuMapping.CreateRegFactuSistemaFacturacionAsync(_receiptJournalRepository.GetEntriesOnOrAfterTimeStampAsync(0).SelectAwait(async x => await _queueItemRepository.GetAsync(x.ftQueueItemId)), _queueItemRepository);
        yield return new JournalResponse
        {
            Chunk = Encoding.UTF8.GetBytes(veriFactu.XmlSerialize()).ToList()
        };
    }
}
