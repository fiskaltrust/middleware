using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// PL-specific journal exports. The international raw exports (0x…0000–0x…0002) are handled by
/// the shared JournalProcessor; the first PL journal type will be JPK_FA (invoice records,
/// Art. 193a Tax Ordinance on-demand structure) — tracked in fiskaltrust/market-pl#53.
/// </summary>
public class JournalProcessorPL : IJournalProcessor
{
    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
    {
        return (new ContentType(MediaTypeNames.Application.Xml), ProcessJpkAsync(request));
    }

    private static async IAsyncEnumerable<byte[]> ProcessJpkAsync(JournalRequest request)
    {
        await Task.CompletedTask;
        yield return Array.Empty<byte>();
    }
}
