using System.Net.Mime;
using System.Text;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class JournalProcessorES : IJournalProcessor
{
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalESRepository;

    public JournalProcessorES(AsyncLazy<IMiddlewareJournalESRepository> journalESRepository)
    {
        _journalESRepository = journalESRepository;
    }

    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
    {
        if (request.ftJournalType == JournalTypeES.VeriFactu.As<JournalType>())
        {
            return (new ContentType(MediaTypeNames.Application.Xml) { CharSet = Encoding.UTF8.WebName }, ProcessVeriFactuAsync(request));
        }

        throw new Exception($"Unsupported journal type: {request.ftJournalType}");
    }

    private async IAsyncEnumerable<byte[]> ProcessVeriFactuAsync(JournalRequest request)
    {
        var journalESs = (await _journalESRepository).GetByTimeStampRangeAsync(request.From, request.To);
        await foreach (var journalES in journalESs)
        {
            if (Enum.TryParse<JournalESType>(journalES.JournalType, out var journalType) && journalType == JournalESType.VeriFactu)
            {
                yield return Encoding.UTF8.GetBytes(journalES.Data);
            }
        }
    }
}
