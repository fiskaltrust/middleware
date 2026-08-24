using System.Net.Mime;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// FR-specific journal exports. The international raw exports (0x...0000-0x...0002) are handled by
/// the shared JournalProcessor; the first FR journal type will be the archive export the tax
/// authority may request (fichier des ecritures / archive NF525), which the v1 QueueFR produces
/// today via its archive receipt.
/// </summary>
public class JournalProcessorFR : IJournalProcessor
{
    public (ContentType, IAsyncEnumerable<byte[]>) ProcessAsync(JournalRequest request)
        => (new ContentType(MediaTypeNames.Application.Xml), ProcessArchiveAsync(request));

    private static async IAsyncEnumerable<byte[]> ProcessArchiveAsync(JournalRequest request)
    {
        await Task.CompletedTask;
        yield return Array.Empty<byte>();
    }
}
