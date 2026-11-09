using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The archive receipt (0x3011), as in v1.2 (<c>ArchiveCommand</c>): signed into the archive chain A
/// against the previous archive receipt's signature, it closes the day and the archive period, prints
/// the totals of the archive period and resets them (market-fr#221, decided 2026-11-09). The chain,
/// the periods and the printed totals follow from the receipt case in <see cref="FRSigningPipeline"/>.
/// </summary>
/// <remarks>
/// Not produced yet: the archive the receipt closes, and the fingerprint of it that the receipt
/// signs. What the fingerprint covers is still open on market-fr#221. The archive keeps its own
/// signature (ADR 0007), so it stays verifiable on its own.
/// </remarks>
public static class FRArchiveReceipt
{
    public static async Task<ProcessCommandResponse?> SignAsync(FRSigningPipeline pipeline, ProcessCommandRequest request)
        => await pipeline.SignAsync(request).ConfigureAwait(false);
}
