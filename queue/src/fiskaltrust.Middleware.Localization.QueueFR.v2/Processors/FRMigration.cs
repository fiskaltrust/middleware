using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The half migration of a v1.2 queue to v2 (market-fr ADR 0003, 0009, 0012): the queue, its chains,
/// counters and totals continue, its receipts stay where they are. A v1.2 queue that has not been
/// migrated accepts only the two migration receipts, both signed on the technical chain E:
/// <list type="bullet">
/// <item><c>0x4021</c> Initiate migration checks that the v1 queue can be migrated and signs, with no other change;</item>
/// <item><c>0x4022</c> Finish migration checks again, signs the migration event (each v1 chain's last
/// counter and hash) and then hands the v1 state over once (<see cref="FRV1Handover"/>).</item>
/// </list>
/// Both are refused once the queue is migrated, and on a queue v2 started itself.
/// </summary>
/// <remarks>
/// TODO market-fr#192: whether the migration event also becomes a JET 210 row (a JET item asking the POS
/// for a 0x3001) is open; nothing is raised until it is decided.
/// </remarks>
public class FRMigration
{
    /// <summary>How far back stored v1 queue items are read for the last v1 signatures and the last v1 sale.</summary>
    public const int MaxV1LookBack = 1000;

    private const long V1ReceiptSignatureType = 0x4652_0000_0000_0001;

    private readonly FRSigningPipeline _pipeline;
    private readonly FRChainStateProvider _chains;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository>? _queueItems;

    public FRMigration(FRSigningPipeline pipeline, FRChainStateProvider chains, AsyncLazy<IMiddlewareQueueItemRepository>? queueItems = null)
    {
        _pipeline = pipeline;
        _chains = chains;
        _queueItems = queueItems;
    }

    public async Task<ProcessCommandResponse?> InitiateAsync(ProcessCommandRequest request)
    {
        var v1 = await ReadV1ItemsAsync().ConfigureAwait(false);
        var refusal = await _chains.ReadAsync(queueFR => Refusal(request, queueFR, v1)).ConfigureAwait(false);
        if (refusal is not null)
        {
            return Refused(request, refusal);
        }

        return await _pipeline.SignAsync(request).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse?> FinishAsync(ProcessCommandRequest request)
    {
        var v1 = await ReadV1ItemsAsync().ConfigureAwait(false);
        var (refusal, migrationEvent) = await _chains.ReadAsync(queueFR => (Refusal(request, queueFR, v1), FRV1Handover.MigrationEvent(queueFR, v1.LastSignatures))).ConfigureAwait(false);
        if (refusal is not null)
        {
            return Refused(request, refusal);
        }

        var stateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.Migration = migrationEvent;
        request.ReceiptResponse.ftStateData = stateData;

        var response = await _pipeline.SignAsync(request).ConfigureAwait(false);
        if (FRSigningPipeline.Succeeded(response))
        {
            // Applied with the signed entry: committed when its queue item is stored, dropped if not,
            // and replayed from the stored entry if the store's outcome is unknown (FRChainRepair).
            await _chains.ApplyToSignedStateAsync(queueFR => FRV1Handover.Migrate(queueFR, migrationEvent, response.receiptResponse.ftReceiptMoment)).ConfigureAwait(false);
        }

        return response;
    }

    private static string? Refusal(ProcessCommandRequest request, ftQueueFR queueFR, V1Items v1)
    {
        if (request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return "A migration receipt cannot be sent in training mode: it moves the fiscal queue to v2.";
        }

        if (!FRV1Handover.AwaitsMigration(queueFR))
        {
            return FRStateV2.Read(queueFR).MigratedAt is { } migratedAt
                ? $"The queue was already migrated to v2 at {migratedAt:u}."
                : "The queue holds no v1.2 state to migrate: it was started by v2.";
        }

        return FRV1Handover.Refusal(queueFR, v1.LastDayEntryMoment);
    }

    private static ProcessCommandResponse Refused(ProcessCommandRequest request, string reason)
    {
        request.ReceiptResponse.SetReceiptResponseError(reason);
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

    /// <summary>What stored v1.2 queue items tell, when the v2 queue's storage still holds them (none in the cloud cashbox).</summary>
    private sealed record V1Items(DateTime? LastDayEntryMoment, IReadOnlyDictionary<string, (long Counter, string Signature)> LastSignatures);

    private async Task<V1Items> ReadV1ItemsAsync()
    {
        var signatures = new Dictionary<string, (long, string)>(StringComparer.Ordinal);
        DateTime? lastDayEntry = null;
        if (_queueItems is null)
        {
            return new V1Items(null, signatures);
        }

        var queueItems = await _queueItems;
        var last = await queueItems.GetLastQueueItemAsync().ConfigureAwait(false);
        var seen = 0;
        for (var row = last?.ftQueueRow ?? 0; row > 0 && seen < MaxV1LookBack; row--, seen++)
        {
            var item = row == last!.ftQueueRow ? last : await queueItems.GetByQueueRowAsync(row).ConfigureAwait(false);
            if (item is null || string.IsNullOrEmpty(item.request) || string.IsNullOrEmpty(item.response))
            {
                continue;
            }

            ReceiptRequest? request;
            ReceiptResponse? response;
            try
            {
                request = JsonSerializer.Deserialize<ReceiptRequest>(item.request);
                response = JsonSerializer.Deserialize<ReceiptResponse>(item.response);
            }
            catch (JsonException)
            {
                continue;
            }

            // Only v1.2 entries: a v2 response carries version 2 in ftState.
            if (request is null || response is null || (((ulong) response.ftState >> 44) & 0xF) == 2 || (((ulong) response.ftState) & 0xFFFF_FFFF) is 0xEEEE_EEEE or 0xFFFF_FFFF)
            {
                continue;
            }

            // v1.2 counts its tickets (0x01) and invoices (0x03) into the day.
            if (lastDayEntry is null && ((long) request.ftReceiptCase & 0xFFFF) is 0x0001 or 0x0003)
            {
                lastDayEntry = item.ftQueueMoment;
            }

            var identification = response.ftReceiptIdentification ?? "";
            var suffix = identification[(identification.LastIndexOf('#') + 1)..];
            var signature = response.ftSignatures?.FirstOrDefault(x => (long) x.ftSignatureType == V1ReceiptSignatureType)?.Data;
            if (suffix.Length > 1 && long.TryParse(suffix[1..], out var counter) && !string.IsNullOrEmpty(signature) && !signatures.ContainsKey(suffix[..1]))
            {
                signatures[suffix[..1]] = (counter, signature[(signature.LastIndexOf('.') + 1)..]);
            }
        }

        return new V1Items(lastDayEntry, signatures);
    }
}
