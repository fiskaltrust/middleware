using System.IO.Pipelines;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.MasterData;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class JournalProcessorES : IJournalProcessor
{
    private readonly AsyncLazy<IMiddlewareRepository<ftJournalES>> _journalESRepository;

    public JournalProcessorES(AsyncLazy<IMiddlewareRepository<ftJournalES>> journalESRepository)
    {
        _journalESRepository = journalESRepository;
    }

    public Task<(ContentType, PipeReader)> ProcessAsync(JournalRequest request)
    {
        if (request.ftJournalType == JournalTypeES.VeriFactu.As<JournalType>())
        {
            return ProcessVeriFactuAsync(request);
        }

        throw new Exception($"Unsupported journal type: {request.ftJournalType}");
    }

    private async Task<(ContentType, PipeReader)> ProcessVeriFactuAsync(JournalRequest request)
    {
        Pipe response = new();
        var journalESs = (await _journalESRepository).GetByTimeStampRangeAsync(request.From, request.To);

        await foreach (var journalES in journalESs)
        {
            if (Enum.TryParse<JournalESType>(journalES.JournalType, out var journalType) && journalType == JournalESType.VeriFactu)
            {
                await response.Writer.WriteAsync(Encoding.UTF8.GetBytes(journalES.Data));
                await response.Writer.WriteAsync(Encoding.UTF8.GetBytes(","));
            }
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await response.Writer.WriteAsync(Encoding.UTF8.GetBytes("["));

            }
            finally
            {
                await response.Writer.CompleteAsync();
            }
        });

        return (new ContentType(MediaTypeNames.Application.Xml) { CharSet = Encoding.UTF8.WebName }, response.Reader);
    }
}
