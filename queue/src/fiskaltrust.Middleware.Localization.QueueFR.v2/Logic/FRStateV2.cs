using System.Text.Json;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The QueueFR v2 state that has no v1 column, kept as JSON in ftQueueFR <c>StateV2</c> and written
/// with the rest of the chain state (market-fr ADR 0012): the archive file still open (ADR 0018) and
/// the moment a v1.2 queue was migrated to v2 (ADR 0003).
/// </summary>
public sealed class FRStateV2
{
    /// <summary>The archive file still open, see <see cref="FROpenArchiveFile"/>.</summary>
    public FROpenArchiveFile.FileState? OpenArchiveFile { get; set; }

    /// <summary>When the finish-migration receipt (0x4022) applied the v1 handover; null on a queue never migrated.</summary>
    public DateTime? MigratedAt { get; set; }

    public static FRStateV2 Read(ftQueueFR queueFR)
        => string.IsNullOrEmpty(queueFR.StateV2) ? new FRStateV2() : JsonSerializer.Deserialize<FRStateV2>(queueFR.StateV2) ?? new FRStateV2();

    public static void Update(ftQueueFR queueFR, Action<FRStateV2> change)
    {
        var state = Read(queueFR);
        change(state);
        queueFR.StateV2 = JsonSerializer.Serialize(state);
    }
}
