using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// FR-specific journal exports. Only journal types qualified with the French country code reach
/// this processor - the international raw exports (action journal, receipt journal, queue items,
/// configuration) are served by the shared <see cref="JournalProcessor"/> and are unaffected.
/// </summary>
/// <remarks>
/// The FR-specific export (the NF525 archive, which the v1 QueueFR produces through its archive
/// receipt) is not implemented yet. It refuses the request instead of returning an empty document:
/// an export that silently yields a zero-length file reads like a queue with no records, which is
/// exactly the wrong answer to give an auditor.
/// </remarks>
public class JournalProcessorFR : IJournalProcessor
{
    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
        => throw new NotImplementedException(
            $"The French journal type 0x{(long) request.ftJournalType:X} is not implemented in the QueueFR.v2 localization yet. " +
            "The international exports (action journal, receipt journal, queue items, configuration) are available and unaffected.");
}
