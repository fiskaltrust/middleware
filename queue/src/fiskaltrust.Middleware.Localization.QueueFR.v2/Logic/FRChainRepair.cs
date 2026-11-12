using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Rolls the chain state in ftQueueFR forward over signed entries that are stored but that the state
/// does not cover yet: a process stopped, or a write failed, between storing the signed queue item and
/// storing ftQueueFR (market-fr ADR 0012). It replays what the signing pipeline does once an entry is
/// signed - the chain position, the period totals and closings, the late-signing range and the pending
/// messages - from the stored request and response, never by re-signing.
/// </summary>
public static class FRChainRepair
{
    /// <summary>A stored queue item read as a signed entry of a v2 chain.</summary>
    public sealed record SignedEntry(ftQueueItem Item, ReceiptRequest Request, ReceiptResponse Response, FRReceiptChain Chain, long Number, string Signature);

    /// <summary>What a stored queue item is to the repair.</summary>
    public enum Kind
    {
        /// <summary>Not a signed v2 entry: unfinished, failed, or not signed into a chain.</summary>
        Skip,

        /// <summary>A signed v2 entry.</summary>
        Signed,

        /// <summary>An entry of a v1 queue (before a half migration): nothing older is v2.</summary>
        V1,
    }

    public static (Kind kind, SignedEntry? entry) Read(ftQueueItem item)
    {
        if (string.IsNullOrEmpty(item.request) || string.IsNullOrEmpty(item.response))
        {
            return (Kind.Skip, null);
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
            return (Kind.Skip, null);
        }

        if (request is null || response is null)
        {
            return (Kind.Skip, null);
        }

        // A v2 queue answers with version 2 in ftState (SignProcessor); a v1.2 entry does not.
        if ((((ulong) response.ftState >> 44) & 0xF) != 2)
        {
            return (Kind.V1, null);
        }

        if (response.ftState.IsState(State.Error))
        {
            return (Kind.Skip, null);
        }

        var chain = request.ResolveChain();
        var identification = response.ftReceiptIdentification ?? "";
        var suffix = identification[(identification.LastIndexOf('#') + 1)..];
        if (!suffix.StartsWith(chain.Identifier(), StringComparison.Ordinal)
            || !long.TryParse(suffix[chain.Identifier().Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return (Kind.Skip, null);
        }

        var signature = response.ftSignatures?.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature))?.Data;
        if (string.IsNullOrEmpty(signature))
        {
            return (Kind.Skip, null);
        }

        return (Kind.Signed, new SignedEntry(item, request, response, chain, number, FRChaining.ChainedSignatureOf(signature)));
    }

    /// <summary>
    /// Applies one signed entry to the state, as <c>FRSigningPipeline</c> did when it signed it, and
    /// returns the files the one-year limit cut at that entry (their journal entries are the caller's).
    /// </summary>
    /// <param name="integrityDefects">Receives the pending JET 90 journal of an entry whose signed content disagrees with its
    /// unsigned state copy; the caller stores it. Null to skip the comparison.</param>
    /// <exception cref="InvalidOperationException">A finish-migration entry signs no readable migration event: whether it was
    /// stored as signed is unknown, so the state is not rolled over it.</exception>
    public static List<FROpenArchiveFile.ForcedFile> Apply(ftQueueFR queueFR, SignedEntry entry, List<ftActionJournal>? integrityDefects = null)
    {
        var (_, request, response, chain, number, signature) = entry;
        queueFR.SetNumerator(chain, number);
        queueFR.SetLastSignature(chain, signature);

        var totals = new FRPeriodTotalsAccumulator(queueFR);
        totals.Add(request);
        foreach (var period in FRPeriodTotalsAccumulator.PeriodsClosedBy(request))
        {
            totals.Close(period, response.ftReceiptMoment, response.ftQueueItemID);
        }

        var training = request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training);
        var zeroReceipt = !training && request.ftReceiptCase.Case() == ReceiptCase.ZeroReceipt0x2000;

        var lateSigning = new FRLateSigningState(queueFR);
        if (!training && request.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning))
        {
            lateSigning.Add(response.ftQueueItemID, request.cbReceiptMoment);
        }
        if (zeroReceipt && lateSigning.IsOpen)
        {
            lateSigning.Close();
        }

        // A zero receipt that still answered "message pending" did not hand the messages over. As in
        // the pipeline, this comes before a forced archive the same entry may raise.
        var pending = new FRPendingMessagesState(queueFR);
        if (zeroReceipt && pending.AnyPending && !response.ftState.IsFlag(StateFlags.MessageIsPending))
        {
            pending.Reset();
        }

        // A finish-migration receipt hands the v1 state over with its signed entry (ADR 0003), read from what was signed
        // (archive-v2 §5: data-set field 20, or the InfoCert payload), never from the unsigned copy in ftStateData.
        if (!training && request.ftReceiptCase.Case() == FRMigrationGate.FinishMigration)
        {
            var migrationEvent = SignedMigration(response);
            if (integrityDefects is not null && !SameEvent(FRStateData.Migration(response), migrationEvent))
            {
                integrityDefects.Add(new FRPendingMessagesState(queueFR).Raise(response.ftQueueID, FRChainIntegrity.IntegrityDefect, FRChainIntegrity.IntegrityDefectLabel,
                    FRJetItems.Information(("ftQueueItemId", response.ftQueueItemID.ToString()), ("defect", "the stored migration event differs from the signed one")), response.ftReceiptMoment));
            }
            FRV1Handover.Migrate(queueFR, migrationEvent, response.ftReceiptMoment);
        }

        return new FROpenArchiveFile(queueFR).Add(request, response, FRArchiveDue.CertificateSerialNumberOf(response));
    }

    /// <summary>The migration event the entry's receipt QR code signs.</summary>
    /// <exception cref="InvalidOperationException">It signs none that reads.</exception>
    private static FRMigrationEvent SignedMigration(ReceiptResponse response)
    {
        var qrCode = response.ftSignatures?.FirstOrDefault(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature))?.Data ?? "";
        try
        {
            return new FRMigrationEvent
            {
                Chains = SignedMigrationEvent.Read(qrCode)
                    .Select(x => new FRV1ChainHead { Chain = x.Chain, Counter = x.Counter, LastHash = x.LastHash, LastSignature = x.LastSignature })
                    .ToList(),
            };
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"The finish-migration entry {response.ftQueueItemID} signs no readable migration event ({ex.Message}); the French chain state is not rolled over it.", ex);
        }
    }

    private static bool SameEvent(FRMigrationEvent? stored, FRMigrationEvent signed)
        => stored is not null
            && stored.Chains.Select(Key).SequenceEqual(signed.Chains.Select(Key));

    private static (string, long, string, string) Key(FRV1ChainHead head) => (head.Chain, head.Counter, head.LastHash ?? "", head.LastSignature ?? "");
}
