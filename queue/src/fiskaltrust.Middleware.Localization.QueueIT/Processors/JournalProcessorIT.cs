using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// The country independent journals (action journal, receipt journal, queue items, configuration) are served by the
/// shared <see cref="JournalProcessor"/>. Italy has no country specific export yet, so an Italian journal type is
/// refused rather than answered with an empty document.
/// </summary>
public class JournalProcessorIT : IJournalProcessor
{
    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
        => throw new NotSupportedException($"The journal type 0x{(ulong) request.ftJournalType:X} is not supported by the QueueIT implementation.");
}
