- Feature Name: `close_transactions_on_daily_closing`
- Start Date: 2023-10-18
- RFC PR: [fiskaltrust/middleware#211](https://github.com/fiskaltrust/middleware/pull/211)
- Tracking Issue: [fiskaltrust/middleware#212](https://github.com/fiskaltrust/middleware/issues/212)
- Markets: `DE`

# Summary

We want to introduce a ftReceiptCaseFlag to the Daily-Closing that closes all open transactions on the TSE.

# Motivation

Open transactions on the TSE can lead to lots of problems and it already is our recommendation to close open transactions before performing the Daily-Closing.
Right now this has to be done manually so this feature makes it more easy for PosCreators to do so.
Originally we intended the Daily-Closing to have this behaviour per default.

# Guide-level explanation

If the the ftReceiptCaseFlag `0x0000000200000000` is set when sending a Daily-Closing all open transactions on the TSE will be closed prior to executing the daily closing.

> ***Example:***
> 
> 1. A Zero-Receipt Request with the ftReceiptCase `0x4445000100800002` (Zero-Receipt with Request TSE-Info flag `0x800000`) is sent and in the Response we see there are three open transactions on the TSE.
>    ```json
>    {
>      "CurrentStartedTransactionNumbers": [ 10, 12, 13 ]
>    }
>    ```
> 2. A Daily-Closing Request with the ftReceiptCase `0x4445000300000007` (Daily-Closing with Close Open Transactions flag `0x200000000`) is sent.
>    The open transactions are closed by the middleware.
> 3. A Zero-Receipt Request with the ftReceiptCase `0x4445000100800002` is sent again in the Response there are no open transactions on the TSE.
>    ```json
>    {
>      "CurrentStartedTransactionNumbers": []
>    }
>    ```

# Reference-level explanation

The Daily-Closing already gets the TSE-Info from the TSE and the middleware already has the ability to close open transactions on the TSE as done in the Fail-Transaction Receipt.

We now introduce a check for the new flag and close the transactions if it is set.
We then close the open transactions on the TSE, add the signatures to the receipt and create an single ActionJournal entry for all transaction that were closed.

```cs
// pseudo code
if (request.HasCloseOpenTransactionsOnTseFlag() && tseInfo.CurrentStartedTransactionNumbers!.Any())
{
    var openSignatures = await CloseOpenTransactionsOnTseAsync(tseInfo.CurrentStartedTransactionNumbers);
    receiptResponse.ftSignatures.AddRange(openSignatures);
    actionJournals.Add(
        new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = queueItem.ftQueueId,
            ftQueueItemId = queueItem.ftQueueItemId,
            Moment = DateTime.UtcNow,
            Priority = -1,
            TimeStamp = 0,
            Message = $"Closed all open transactions on the TSE {tseInfo.SerialNumberOctet}.",
            Type = $"{0x4445_0000_2000_0000:X}-{nameof(tseInfo.CurrentStartedTransactionNumbers)}",
            DataJson = JsonConvert.SerializeObject(tseInfo.CurrentStartedTransactionNumbers)
        }
    );
}
```

The method `CloseOpenTransactionsOnTseAsync` looks like this which is inspired by the current [Fail-Transaction Receipt](https://github.com/fiskaltrust/middleware/blob/1a9abd80430e9dfecdd17289024e9d19e798d19b/queue/src/fiskaltrust.Middleware.Localization.QueueDE/RequestCommands/FailTransactionReceiptCommand.cs#L64-L77).

```cs
// pseudo code
private async Task<IEnumerable<SignaturItem>> CloseOpenTransactionsOnTseAsync(IEnumerable<long> openTransactionsOnTSE, ftQueueItem queueItem, ftQueueDE queueDE)
{
  var openSignatures = new List<SignaturItem>();
  var openTransactionsOnQueue = (await _openTransactionRepo.GetAsync().ConfigureAwait(false)).ToList();

  foreach (var openTransactionOnTSE in openTransactionsOnTSE)
  {
      (var openProcessType, var openPayload) = _transactionPayloadFactory.CreateAutomaticallyCanceledReceiptPayload();
      var finishResult = await _transactionFactory.PerformFinishTransactionRequestAsync(openProcessType, openPayload, queueItem.ftQueueItemId, queueDE.CashBoxIdentification, openTransactionOnTSE).ConfigureAwait(false);
      openSignatures.AddRange(_signatureFactory.GetSignaturesForFinishTransaction(finishResult));
      var openTransactionOnQueue = openTransactionsOnQueue.FirstOrDefault(x => (ulong) x.TransactionNumber == openTransactionOnTSE);
      if (openTransactionOnQueue != null)
      {
          await _openTransactionRepo.RemoveAsync(openTransactionOnQueue.cbReceiptReference).ConfigureAwait(false);
      }
  }
  return openSignatures;
}
```

It goes through all open transactions on the TSE and closes them on the TSE (`_transactionFactory.PerformFinishTransactionRequestAsync`).
Then it removes the open transaction from the Queue if it was also open there (`_openTransactionRepo.RemoveAsync`).

If a transaction is marked open in the Queue in the `OpenTransactionRepository` but already closed on the TSE this must not lead to an exception when the Queue later wants to close this transaction.

# Drawbacks

## Latency

If there are hundreds of open transaction on the TSE closing them all can take a while.
This could lead to a timeout or long waiting times when performing a Daily-Closing.

## Closing foreign Transactions

If a TSE is used by multiple Queues the Transactions of open other Queues will be closed as well.
This might not be desired so this flag should be used with that in mind.

# Rationale and alternatives

This feature simplifies the process of closing open transactions on the TSE which is something that is already recommended to do and made easier by this flag.
It does not introduce anything drastically new.

The alternative is to continue using the current process of closing open transactions manually described in [Prior art](#prior-art) below.

# Prior art

The same effect can be achieved manually right now by following the process outlined in this [KBA](https://portal.fiskaltrust.de/KBArticle#/KA-01062/Force-%3Cspan%20class=%22highlight%22%3Eclosing%3C/span%3E%20open%20transactions).

1. Send a "Zero receipt with TSE info" Receipt to to the queue. The `ftStateData` property of the response will then contain the `CurrentStartedTransactionNumbers`.
2. Then close this with a "Fail-transaction receipt (Multiple transactions)" Request.

# Unresolved questions

* Is it possible to only close transactions of the current Queue and would this be desirable?
