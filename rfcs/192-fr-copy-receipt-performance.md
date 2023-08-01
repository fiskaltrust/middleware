- Feature Name: `fr_copy_receipt_performance`
- Start Date: 2023-08-01
- RFC PR: [fiskaltrust/middleware#192](https://github.com/fiskaltrust/middleware/pull/192)
- Tracking Issue: [fiskaltrust/middleware#0000](https://github.com/fiskaltrust/middleware/issues/0000)

# Summary

Adding a `ftJournalFRCopyReceipts` table in the FR Queue where we store copy receipts.

# Motivation

The current implementation of the CopyReceipt is performing very badly.
The performance of CopyReceipts get worse for each previous CopyReceipt in the queue.

Currently when processing a CopyReceipt we have iterate over all previous CopyReceipts in the `ftJournalFR` table, deserialize the response and check if it is using the same `cbPreviousReceiptReference`.
We then count all of those CopyReceipts and return that as the count.


# Guide-level explanation

For the PosCreator they are no implementation changes needed, the CopyReceipts still work like they did before.

## New Local Queues

New Queues don't need to take anything into account.
They will work out of the box.

## Updating Local Queues

The new table will be created on startup after updating the queue.

The data will be migrated from the `ftJournalFR` table to the new `ftJournalFRCopyReceipts` table.
This will take roughly the same time as it takes to process a CopyReceipt with the old implementation.

## SignatureCloud

The SignatureCloud will be updated aswell.
The new version will not automatically be used since this would mean a recertification of the PosCreator.
Insted we will time the switch to the new version accordingly with the PosCreators.

# Reference-level explanation

We will add a migration which adds the new table `ftJournalFRCopyReceipts`.

| Column Name                  | Type     | Description                                          |
|------------------------------|----------|------------------------------------------------------|
| `cbPreviousReceiptReference` | `string` | The `cbPreviousReceiptReference` of the CopyReceipt. |
| `ftQueueItemID`              | `Guid`   | The `ftQueueItemID` of the CopyReceipt.              |

This table contains one row for each CopyReceipt. To get the cound of copies of one receipt we can simply count the rows with the same `cbPreviousReceiptReference`.

When a new CopyReceipt is processed we take the cound of rows with the same `cbPreviousReceiptReference` and return that plus one as the count.
Then we insert the new CopyReceipt into the `ftJournalFRCopyReceipts` table.

```cs
private async Task<int> GetCountOfExistingCopiesAsync(string cbPreviousReceiptReference)
{
  return _ftJournalFRCopyReceiptsRepository.GetCountOfCopiesAsync(cbPreviousReceiptReference);
}
```

## Migration

The new table will be created with the usual migration process.

On startup of the Queue (in the 1.2 worker) we check if the table contains any entries and if not we will populate it with the old data from the `ftJournalFR` table.

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
                _ftJournalFRCopyReceiptsRepository.Insert(new FtJournalFRCopyReceipt
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

### AzureTableStorage

The Azure Table Storage Queue will have the `cbPreviousReceiptReference` as a Partition key and the `ftQueueItemID` as a Row key.

This means counting all rows with the same `cbPreviousReceiptReference` will be very fast.

> This is the technical portion of the RFC.
> Try to capture the broad implementation strategy,
> and then focus in on the tricky details so that:
> 
> - Its interaction with other features is clear.
> - It is reasonably clear how the feature would be implemented.
> - Corner cases are dissected by example.
> - Discuss how this impacts the ability to read, understand, and maintain middleware code. Code is read and modified far more often than written; will the proposed feature make code easier to maintain?
> 
> When necessary, this section should return to the examples given in the previous section and explain the implementation details that make them work.
> 
> When writing this section be mindful of the following:
> - **RFCs should be scoped:** Try to avoid creating RFCs for huge design spaces that span many features. Try to pick a specific feature slice and describe it in as much detail as possible. Feel free to create multiple RFCs if you need multiple features.
> - **RFCs should avoid ambiguity:** Two developers implementing the same RFC should come up with nearly identical implementations.
> - **RFCs should be "implementable":** Merged RFCs should only depend on features from other merged RFCs and existing features. It is ok to create multiple dependent RFCs, but they should either be merged at the same time or have a clear merge order that ensures the "implementable" rule is respected.

# Drawbacks

This solution involves a new table and we need to populate it with the data from the `ftJournalFR` table before we can use the new algorithm.

> Why should we *not* do this?

# Rationale and alternatives

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
> - Why is this design the best in the space of possible designs?
> - What other designs have been considered and what is the rationale for not choosing them?
> - What objections immediately spring to mind? How have you addressed them?
> - What is the impact of not doing this?

# \[Optional\] Prior art

> Discuss prior art, both the good and the bad, in relation to this proposal.
> A few examples of what this can include are:
> 
> - Does this feature exist in other markets and what experience have their community had?
> - Does this feature exist in other possystems and what experience have their community had?
> - Papers: Are there any published papers or great posts that discuss this? If you have some relevant papers to refer to, this can serve as a more detailed theoretical background.
> 
> This section is intended to encourage you as an author to think about the lessons from other markets and projects, provide readers of your RFC with a fuller picture.
> If there is no prior art, that is fine - your ideas are interesting to us whether they are brand new or not.
> 
> Note that while precedent set by other projects and markets is some motivation, it does not on its own motivate an RFC.

# Unresolved questions

> - What parts of the design do you expect to resolve through the RFC process before this gets merged?
> - What parts of the design do you expect to resolve through the implementation of this feature before before the feature PR is merged?
> - What related issues do you consider out of scope for this RFC that could be addressed in the future independently of the solution that comes out of this RFC?

# \[Optional\] Future possibilities

> Think about what the natural extension and evolution of your proposal would be and how it would affect the middleware and ecosystem as a whole in a holistic way.
> Try to use this section as a tool to more fully consider all possible interactions with the project in your proposal.
> Also consider how this all fits into the roadmap for the project and of the relevant sub-team.
> 
> This is also a good place to "dump ideas", if they are out of scope for the RFC you are writing but otherwise related.
> 
> Note that having something written down in the future-possibilities section is not a reason to accept the current or a future RFC;
> such notes should be in the section on motivation or rationale in this or subsequent RFCs.
> The section merely provides additional information.