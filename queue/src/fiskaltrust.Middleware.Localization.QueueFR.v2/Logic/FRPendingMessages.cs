using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// JET events found outside a request (an integrity failure in a local archive export, a rollback),
/// delivered the way v1.2 delivered its messages (decided 2026-11-07, market-fr#200): the finding is an
/// action journal entry and raises the v1 <c>ftQueueFR.MessageCount</c>/<c>MessageMoment</c> columns;
/// every response then carries <see cref="StateFlags.MessageIsPending"/>; the next non-training zero
/// receipt returns each pending event as a JET item, which the POS sends as a <c>0x3001</c> (ADR 0009),
/// and resets the count.
/// <para>
/// v2 writes every action journal with priority -1, so a pending JET event is recognised by its
/// <see cref="JournalTypeSuffix"/>, not by its priority as in v1.2.
/// </para>
/// </summary>
public class FRPendingMessagesState
{
    public const string JournalTypeSuffix = "-JetPending";

    private readonly ftQueueFR _queueFR;

    public FRPendingMessagesState(ftQueueFR queueFR)
    {
        _queueFR = queueFR;
    }

    public bool AnyPending => _queueFR.MessageCount > 0;

    public DateTime? Since => _queueFR.MessageMoment;

    /// <summary>
    /// Records a JET event as pending and returns the action journal entry that carries it. The caller
    /// stores the entry; the count is stored with ftQueueFR.
    /// </summary>
    public ftActionJournal Raise(Guid queueId, int code, string label, string? information, DateTime moment)
    {
        _queueFR.MessageCount++;
        _queueFR.MessageMoment ??= moment;
        return new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = queueId,
            ftQueueItemId = Guid.Empty,
            Type = $"{code}{JournalTypeSuffix}",
            Moment = moment,
            Message = $"JET {code} pending: {label}",
            Priority = -1,
            DataJson = JsonSerializer.Serialize(new PendingJetEvent(code, label, information)),
        };
    }

    public void Reset()
    {
        _queueFR.MessageCount = 0;
        _queueFR.MessageMoment = null;
    }

    public sealed record PendingJetEvent(int Code, string Message, string? Information);
}

/// <summary>Applies the pending messages to a signed response (see <see cref="FRPendingMessagesState"/>).</summary>
public class FRPendingMessages
{
    private readonly AsyncLazy<IMiddlewareActionJournalRepository>? _actionJournals;

    public FRPendingMessages(AsyncLazy<IMiddlewareActionJournalRepository>? actionJournals)
    {
        _actionJournals = actionJournals;
    }

    /// <summary>
    /// A non-training zero receipt collects the pending JET events as JET items and clears the count;
    /// any other response is flagged <see cref="StateFlags.MessageIsPending"/> while events are pending.
    /// </summary>
    public async Task ApplySignedAsync(ReceiptRequest request, ReceiptResponse response, FRPendingMessagesState state)
    {
        if (!state.AnyPending)
        {
            return;
        }

        var collects = request.ftReceiptCase.Case() == ReceiptCase.ZeroReceipt0x2000 && !request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training);
        if (!collects || _actionJournals is null)
        {
            response.ftState = response.ftState.WithFlag(StateFlags.MessageIsPending);
            return;
        }

        var from = state.Since?.Ticks ?? 0;
        await foreach (var journal in (await _actionJournals).GetByPriorityAfterTimestampAsync(lowerThanPriority: 0, from).ConfigureAwait(false))
        {
            if (journal.Type?.EndsWith(FRPendingMessagesState.JournalTypeSuffix, StringComparison.Ordinal) != true || string.IsNullOrEmpty(journal.DataJson))
            {
                continue;
            }

            FRPendingMessagesState.PendingJetEvent? pending;
            try
            {
                pending = JsonSerializer.Deserialize<FRPendingMessagesState.PendingJetEvent>(journal.DataJson);
            }
            catch (JsonException)
            {
                continue;
            }

            if (pending is not null)
            {
                response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(pending.Code, pending.Message, pending.Information));
            }
        }

        // A count left by a half-migrated v1.2 queue is cleared too, as a v1.2 zero receipt would; its
        // messages stay in the action journal.
        state.Reset();
    }
}
