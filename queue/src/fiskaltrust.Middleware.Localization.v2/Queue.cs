using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;

#pragma warning disable
namespace fiskaltrust.Middleware.Localization.v2;

public class Queue 
{
    private readonly ISignProcessor _signProcessor;
    private readonly IJournalProcessor _journalProcessor;
    private readonly MiddlewareConfiguration _middlewareConfiguration;

    public Queue(ISignProcessor signProcessor, IJournalProcessor journalProcessor, MiddlewareConfiguration middlewareConfiguration)
    {
        _signProcessor = signProcessor;
        _journalProcessor = journalProcessor;
        _middlewareConfiguration = middlewareConfiguration;
    }

    public async Task<ReceiptResponse> SignAsync(ReceiptRequest request) => await _signProcessor.ProcessAsync(request).ConfigureAwait(false);

    public IAsyncEnumerable<ifPOS.v1.JournalResponse> JournalAsync(ifPOS.v1.JournalRequest request) => _journalProcessor.ProcessAsync(request);
}
