- Feature Name: `fr_copy_receipt_performance`
- Start Date: 2023-08-01
- RFC PR: [fiskaltrust/middleware#192](https://github.com/fiskaltrust/middleware/pull/192)
- Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000)

# Summary

Adding a `ftJournalFRCopyReceipts` table in the FR Queue where we store copy receipts.

# Motivation

The current implementation of the CopyReceipt is performing badly.
Processing a CopyReceipt takes an increasing amount of time for each previous CopyReceipt in the queue.
This inevitably leads to a point where the CopyReceipts are not processed in time.

Currently when processing a CopyReceipt we have iterate over all previous CopyReceipts in the `ftJournalFR` table, deserialize the response and check if it is using the same `cbPreviousReceiptReference`.
We then count all of those CopyReceipts and return that as the count.

# Guide-level explanation

For the PosCreator they are no implementation changes needed, the CopyReceipts still work like they did before.

## New local queues

New Queues don't need to take anything into account.
They will work out of the box.

## Updating local queues

The new table will be created on start up after updating the queue.

The new `ftJournalFRCopyReceipts` table will be populated with data from the `ftJournalFR` table.
This will take a similar timeframe as it takes to process a CopyReceipt with the old implementation.

## SignatureCloud

The SignatureCloud will be updated as well.
The new version will not automatically be used since this would mean a recertification of the PosCreator.
Instead we will time the switch to the new version accordingly with the PosCreators.

# Reference-level explanation

We will add a migration which adds the new table `ftJournalFRCopyReceipts`.

| Column Name                  | Type     | Description                                          |
|------------------------------|----------|------------------------------------------------------|
| `cbPreviousReceiptReference` | `string` | The `cbPreviousReceiptReference` of the CopyReceipt. |
| `ftQueueItemID`              | `Guid`   | The `ftQueueItemID` of the CopyReceipt.              |

This table contains one row for each CopyReceipt. To get the count of copies of one receipt we can simply count the rows with the same `cbPreviousReceiptReference`.

When a new CopyReceipt is processed we take the count of rows with the same `cbPreviousReceiptReference` and return that plus one as the count.
Then we insert the new CopyReceipt into the `ftJournalFRCopyReceipts` table.

The `GetCountOfExistingCopiesAsync` method looks like this:
```cs
private Task<int> GetCountOfExistingCopiesAsync(string cbPreviousReceiptReference)
{
  return _journalFRCopyReceiptsRepository.GetCountOfCopiesAsync(cbPreviousReceiptReference);
}
```

The `GetCountOfCopiesAsync` method of the SQLite `JournalFRCopyReceiptsRepository` looks like this:
```cs
public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference) {
    var query = "SELECT count(*) AS ReceiptCase FROM ftJournalFRCopyReceipts WHERE cbPreviousReceiptReference = @cbPreviousReceiptReference";
    return await DbConnection.QuerySingleAsync<int>(query, new { cbPreviousReceiptReference });
}
```

## Migration

The new table will be created with the usual migration process.

On start up of the Queue (in the 1.2 `workerFR.cs`) we check if the table contains any entries and if not we will populate it with the old data from the `ftJournalFR` table.

The population of the table will be done like this:

```cs
private async Task<int> PopulateFtJournalFRCopyReceiptsTableAsync()
{
    await foreach (var copyJournal in _journalFRRepository.GetProcessedCopyReceiptsAsync())
    {
        var queueItem = await _queueItemRepository.GetAsync(copyJournal.ftQueueItemId);
        var request = queueItem?.request != null ? JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request) : null;
        var response = queueItem?.response != null ? JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response) : null;
        if (response != null)
        {
            var duplicata = response.ftSignatures.FirstOrDefault(x => x.Caption == "Duplicata");

            if (duplicata != null)
            {
                _journalFRCopyReceiptsRepository.Insert(new FtJournalFRCopyReceipt
                {
                    cbPreviousReceiptReference = request.cbPreviousReceiptReference,
                    ftQueueItemId = copyJournal.ftQueueItemId
                });
            }
        }
    }

    return count;
}
```

## SignatureCloud

We will setup a new SignatureCloud environment which uses the new implementation and keep using the old one in the beginning.
We will have a load balancer in front of the SignatureCloud which will route the requests to the correct environment.

When the PosCreator is ready to use the new implementation we will switch the load balancer to the new environment.
This will be done in coordination with the PosCreators and the recertification.

### AzureTableStorage

The Azure Table Storage Queue will have the `cbPreviousReceiptReference` as a Partition key and the `ftQueueItemID` as a Row key.

This means counting all rows with the same `cbPreviousReceiptReference` will be very fast.

# Drawbacks

This solution involves a new table and we need to populate it with the data from the `ftJournalFR` table before we can use the new implementation.
This brings some problems with it especially in the SignatureCloud.

This update version of the middleware might need to be certified again.

# Rationale and alternatives

An alternative way of doing this would be to keep the current implementation and improve it.

One way to do that would be to not count every CopyReceipt in the `ftJournalFR` table but only find the latest one and increase its count by one.

```cs
private async Task<int> GetCountOfExistingCopiesAsync(string cbPreviousReceiptReference)
{
    await foreach (var copyJournal in _journalFRRepository.GetProcessedCopyReceiptsDescAsync())
    {
        var queueItem = await _queueItemRepository.GetAsync(copyJournal.ftQueueItemId);
        var response = queueItem?.response != null ? JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response) : null;
        if (response != null)
        {
            var duplicata = response.ftSignatures.FirstOrDefault(x => x.Caption == "Duplicata" && x.Data.EndsWith($"Duplicata de {cbPreviousReceiptReference}"));

            if (duplicata != null)
            {
                return int.Parse(duplicata.Data.Split('.')[0]);
            }
        }
    }
    return 0;
}
```

In the worst case scenario this would be as slow as the current implementation but in the best case scenario only need one database query.

## Open questions

* Can we implement a `GetProcessedCopyReceiptsDescAsync` method that gets us the CopyReceipts in descending order.
* Is the performance of this solution in a real world scenario enough?
  And do we have clients which have a lot of CopyReceipts and need copies of older receipts where this would lean towards worst case performance?

# Unresolved questions

## Migration

Can we populate the table asynchronously? This would mean we could start the Queue without waiting for the population of the `ftJournalFRCopyReceipts` table to finish.

However this would mean we would have to either fail CopyReceipts until the process of population is complete or wait for the population to finish before we start processing the CopyReceipt.

## SignatureCloud

Will we perform the migration manually before switching the PosCreator to the new environment or will we have the SignatureCloud perform the migration automatically on start up?

# Future possibilities

Adding a new table to the database is not ideal but there will almost certainly more cases in the future where we'll need to to such a change.
One example are the receipt references in the German market where a new table would certainly be helpful.

Implementing this here we would learn a lot about how to do deal with these cases and could use that knowledge in the future.