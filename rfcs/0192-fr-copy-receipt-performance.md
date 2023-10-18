- Feature Name: `fr_copy_receipt_performance`
- Start Date: 2023-08-01
- RFC PR: [fiskaltrust/middleware#192](https://github.com/fiskaltrust/middleware/pull/192)
<!-- - Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000) -->
- Markets: `FR`

# Summary

Adding a `ftJournalFRCopyPayload` table in the FR Queue where we store copy receipts.

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

The new `ftJournalFRCopyPayload` table will be populated with the `CopyPayload` (The deserialized JWT) data of each CopyReceipt from the `ftJournalFR` table.

## SignatureCloud

The SignatureCloud will be updated as well.
The new version will not automatically be used since this would mean a recertification of the PosCreator.
Instead we will time the switch to the new version accordingly with the PosCreators.

# Reference-level explanation

We will add a migration which adds the new table `ftJournalFRCopyPayload`.

This Table will contain the following columns:

| Name                    | Data Type  |
|-------------------------|------------|
| QueueId                 | `Guid`     |
| CashBoxIdentification   | `string`   |
| Siret                   | `string`   |
| ReceiptId               | `string`   |
| ReceiptMoment           | `DateTime` |
| QueueItemId             | `Guid`     |
| CopiedReceiptReference  | `string`   |
| CertificateSerialNumber | `string`   |

This table contains one row for each CopyReceipt and contains the deserialized JWT of the CopyReceipt taken from the `ftJournalFR` table.

To get the count of copies of one receipt we can simply count the rows with the same `CopiedReceiptReference`.

When a new CopyReceipt is processed we count the rows where the `CopiedReceiptReference` equals the `cbPreviousReceiptReference` and return that as the count.
Then we insert the JWT payload of the new CopyReceipt into the `ftJournalFRCopyPayload` table.

The `GetCountOfExistingCopiesAsync` method looks like this:
```cs
private Task<int> GetCountOfExistingCopiesAsync(string cbPreviousReceiptReference)
{
  return _journalFRCopyPayloadRepository.GetCountOfCopiesAsync(cbPreviousReceiptReference);
}
```

The `GetCountOfCopiesAsync` method of the SQLite `JournalFRCopyPayloadRepository` looks like this:
```cs
public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference) {
    var query = "SELECT count(*) FROM ftJournalFRCopyPayload WHERE CopiedReceiptReference = @cbPreviousReceiptReference";
    return await DbConnection.QuerySingleAsync<int>(query, new { cbPreviousReceiptReference });
}
```

## Initialization

The new table will be created with the usual migration process.

On start up of the Queue (in the 1.2 `workerFR.cs`) if the queue has already processed some CopyReceipts we will populate it with the old data from the `ftJournalFR` table.
We can check the number of processed CopyReceipts by reading the `CNumerator` of the `ftQueueFR` table.

The population of the table will be done like this:

```cs
private async Task PopulateFtJournalFRCopyPayloadTableAsync()
{
    foreach (var copyJournal in _journalFRRepository.GetProcessedCopyReceiptsAsync())
    {
        var jwt = copyJournal.JWT.Split('.');
        var payload = JsonConvert.DeserializeObject<CopyPayload>(Encoding.UTF8.GetString(Utilities.FromBase64urlString(jwt[1])));
        _journalFRCopyPayloadRepository.Insert(payload);
    }
}
```

## SignatureCloud

We will setup a new SignatureCloud environment which uses the new implementation and keep using the old one in the beginning.
We will have a load balancer in front of the SignatureCloud which will route the requests to the correct environment.

When the PosCreator is ready to use the new implementation we will switch the load balancer to the new environment.
This will be done in coordination with the PosCreators and the recertification.

### AzureTableStorage

The Azure Table Storage Queue will have the `CopiedReceiptReference` as a Partition key and the `QueueItemId` as a Row key.

This means counting all rows with the same `CopiedReceiptReference` will be very fast.

# Drawbacks

This solution involves a new table and we need to populate it with the data from the `ftJournalFR` table before we can use the new implementation.
This brings some problems with it especially in the SignatureCloud.

This updated version of the middleware might need to be certified again.

# Rationale and alternatives

## Option 1: Improve current implementation

One alternative is to use the current implementation but parse the JWT instead of the `ReceiptResponse`.
This takes `n` DB calls less than the old implementation (where `n` is the number of CopyReceipts in the `ftJournalFR`).
Also the `json` payload we have to deserialize is smaller and less complex.

```cs
private int GetCountOfExistingCopies(string cbPreviousReceiptReference)
{ 
    var count = 0;
    var copyJournals = parentStorage.JournalFRTableByType("C");
    foreach (var journal in copyJournals)
    {
        var jwt = journal.JWT.Split('.');
        var payload = JsonConvert.DeserializeObject<CopyPayload>(Encoding.UTF8.GetString(Utilities.FromBase64urlString(jwt[1])));
        if (payload.CopiedReceiptReference == cbPreviousReceiptReference)
        {
            count++;
        }
    }
    return count;
}
```

> ***Note:** We will release this out first as an intermediate change to provide some initial improvements.*

## Option 2: Search only for the latest CopyReceipt or receipt with the same `cbReceiptReference`

An other alternative way of doing this would be to keep the current implementation and improve it.

One way to do that would be to not count every CopyReceipt in the `ftJournalFR` table but only find the latest one and increase its count by one.

If we find a non copy receipt with the same `cbReceiptReference` as the `cbPreviousReceiptReference` we can return 0 directly.

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

            if(response.cbReceiptReference == cbPreviousReceiptReference) {
                return 0;
            }
        }
    }
    return 0;
}
```

In the worst case scenario this would be as slow as the current implementation but in the best case scenario only need one database query.

### Open questions

* Can we implement a `GetProcessedCopyReceiptsDescAsync` method that gets us the CopyReceipts in descending order.
  Can we sort by row number or `TimeStamp`?
* Taking non copy receipts into account would change the behaviour of the middleware in the case of multiple different receipts with the same receipt reference. Is this acceptable?
* Is the performance of this solution in a real world scenario enough?
  And do we have clients which have a lot of CopyReceipts and need copies of older receipts where this would lean towards worst case performance?

# Unresolved questions

## Initialization

Can we populate the table asynchronously?
This would mean we could start the Queue (and process non CopyReceipts) without waiting for the population of the `ftJournalFRCopyPayload` table to finish.

However this would mean we would have to either fail CopyReceipts until the process of population is complete or wait for the population to finish before we start processing the CopyReceipt.
This could be done using a `Semaphore` in the 1.2 middleware and a `TaskCompletionSource` in the 1.3 middleware.

> ***Note:** This can be investigated and decides during the implementation phase. An initialization time of up to 25 seconds at the first start up should be acceptable.*

## SignatureCloud

Will we perform the migration manually before switching the PosCreator to the new environment or will we have the SignatureCloud perform the migration automatically on start up?

> ***Note:** The SignatureCloud will be tackled separately after the local implementation is rolled out.*

# Future possibilities

Adding a new table to the database is not ideal but there will almost certainly more cases in the future where we'll need to do such a change.
One example are the receipt references in the German market where a new table would certainly be helpful.

Implementing this here we would learn a lot about how to do deal with these cases and could use that knowledge in the future.
