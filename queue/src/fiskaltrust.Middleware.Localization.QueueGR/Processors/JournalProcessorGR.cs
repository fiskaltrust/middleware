using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

#pragma warning disable
public class JournalProcessorGR : IJournalProcessor
{
    private readonly IStorageProvider _storageProvider;

    public JournalProcessorGR(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public IAsyncEnumerable<JournalResponse> ProcessAsync(JournalRequest request)
    {
        throw new NotImplementedException();
    }
}
