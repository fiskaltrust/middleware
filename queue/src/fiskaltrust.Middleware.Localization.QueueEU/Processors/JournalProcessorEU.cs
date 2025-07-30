using System.IO.Pipelines;
using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueEU.Processors;

public class JournalProcessorEU : IJournalProcessor
{

    public JournalProcessorEU()
    {
    }

    (ContentType contentType, IAsyncEnumerable<byte[]> result) IJournalProcessor.ProcessAsync(JournalRequest request) => throw new NotImplementedException();
}
