using System.IO.Pipelines;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using fiskaltrust.ifPOS.v2;
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
    private readonly ILogger<JournalProcessorES> _logger;
    private readonly AsyncLazy<IMiddlewareRepository<ftJournalES>> _journalESRepository;

    public JournalProcessorES(ILogger<JournalProcessorES> logger, AsyncLazy<IMiddlewareRepository<ftJournalES>> journalESRepository)
    {
        _logger = logger;
        _journalESRepository = journalESRepository;
    }

    public Task<(ContentType, PipeReader)> ProcessAsync(JournalRequest request)
    {
        if (request.ftJournalType == 0)
        {
            return ProcessVeriFactuAsync(request);
        }

        throw new Exception($"Unsupported journal type: {request.ftJournalType}");
    }

    private async Task<(ContentType, PipeReader)> ProcessVeriFactuAsync(JournalRequest request)
    {
        Pipe response = new();
        var journalESs = (await _journalESRepository).GetByTimeStampRangeAsync(request.From, request.To);

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var journalES in journalESs)
                {
                    if (Enum.TryParse<JournalESType>(journalES.JournalType, out var journalType) && journalType == JournalESType.VeriFactu)
                    {
                        await response.Writer.WriteAsync(Encoding.UTF8.GetBytes(journalES.RequestData));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing journal request");
            }
            finally
            {
                await response.Writer.CompleteAsync();
            }
        });

        return (new ContentType(MediaTypeNames.Application.Xml) { CharSet = Encoding.UTF8.WebName }, response.Reader);
    }
}
