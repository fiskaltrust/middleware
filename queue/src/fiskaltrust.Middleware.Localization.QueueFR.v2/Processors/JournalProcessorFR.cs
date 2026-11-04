using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// FR-specific journal exports. Only journal types qualified with a country code reach this
/// processor - the international raw exports (action journal, receipt journal, queue items,
/// configuration) are served by the shared <see cref="JournalProcessor"/> and are unaffected.
/// </summary>
/// <remarks>
/// The FR-specific exports (<see cref="JournalTypeFR"/>: the status, the per-group journals and the
/// NF525 archive) are not implemented yet: they wait on the archive chain layout and the shared
/// archive package (fiskaltrust/market-fr#114, #218). Each one refuses the request instead of
/// returning an empty document: an export that silently yields a zero-length file reads like a
/// queue with no records, which is exactly the wrong answer to give an auditor.
/// </remarks>
public class JournalProcessorFR : IJournalProcessor
{
    private const long CountryMask = unchecked((long) 0xFFFF_0000_0000_0000);
    private const long CountryFR = 0x4652_0000_0000_0000;

    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
    {
        var journalType = (long) request.ftJournalType;
        if ((journalType & CountryMask) != CountryFR)
        {
            throw new ArgumentException($"The journal type 0x{journalType:X} is not a French journal type and is not supported by the QueueFR.v2 localization.");
        }

        var journalTypeFR = request.ftJournalType.TypeFR();
        if (!journalTypeFR.IsKnown())
        {
            throw new ArgumentException($"The French journal type 0x{journalType:X} is unknown. The French journal types are listed in {nameof(JournalTypeFR)}.");
        }

        throw new NotImplementedException(
            $"The French journal type 0x{journalType:X} ({journalTypeFR}) is not implemented in the QueueFR.v2 localization yet. " +
            "The international exports (action journal, receipt journal, queue items, configuration) are available and unaffected.");
    }
}
