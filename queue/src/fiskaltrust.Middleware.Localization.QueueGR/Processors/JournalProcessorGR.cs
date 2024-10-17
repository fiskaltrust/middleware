using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class JournalProcessorGR : IJournalProcessor
{
    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        // TODO integrate SAFT
        throw new NotImplementedException();
    }
}
