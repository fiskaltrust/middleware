using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
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

    /// <summary>Applies one signed entry to the state, as <c>FRSigningPipeline</c> did when it signed it.</summary>
    public static void Apply(ftQueueFR queueFR, SignedEntry entry)
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

        // A zero receipt that still answered "message pending" did not hand the messages over.
        var pending = new FRPendingMessagesState(queueFR);
        if (zeroReceipt && pending.AnyPending && !response.ftState.IsFlag(StateFlags.MessageIsPending))
        {
            pending.Reset();
        }
    }
}
