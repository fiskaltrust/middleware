# Middleware data access flows
The access pattern the Middleware currently uses to communicate with its database when different operations are executed. May be helpful when designing new storage layers.

Currently, this only applies to the German MW. May need to be extended when we've finished migrating other markets.

## Sign operations
### Regular receipt (implicit)
1. **[Read]** Queue by ID
2. **[Write]** QueueItem
3. **[Write]** Queue
4. **[Read]** QueueDE by ID
5. **[Write]** OpenTransaction
6. **[Read]** OpenTransaction by ID
7. **[Write]** OpenTransaction
9. **[Write]** QueueDE
10. **[Write]** QueueItem
11. **[Write]** Queue
12. **[Write]** ReceiptJournal
13. **[Write]** Queue
14. **[Write]** ActionJournals

### Receipt request
1. **[Read]** Queue by ID
2. **[Read]** QueueItem by `cbReceiptReference` and `cbTerminalID`
3. **[Write]** ActionJournals

### Zero receipts
1. **[Read]** Queue by ID
2. **[Write]** QueueItem
3. **[Write]** Queue
4. **[Read]** QueueDE by ID
5. **[Read]** FailedFinishTransactions (all)
6. **[Write]** FailedFinishTransaction (n times)
7. **[Read]** FailedFinishTransactions (all)
8. `foreach` FailedFinishTransactions
   1. **[Read]** QueueItem by ID
   2. **[Read]** OpenTransaction by ID
   3. **[Write]** OpenTransaction
   4. **[Write]** FailedFinishTransaction
9. **[Read]** FailedStartTransactions (all)
10. `foreach` FailedStartTransactions
   1. **[Write]** OpenTransaction
   2. **[Write]** FailedStartTransaction
11. **[Read]** SignatureCreationUnitDE by ID
12. **[Write]** SignatureCreationUnitDE
13. **[Write]** JournalDE

## Journal operations
### Entity-specific journals
1. **[Read]** All respective entities (ActionJournal, ReceiptJournal, ...)

### DSFinV-K
1. **[Read]** QueueDE by ID
1. **[Read]** ftSignatureCreationUnit by ID
2. **[Read]** All ActionJournals
3. **[Read]** All ReceiptJournals
4. **[Read]** All QueueItems
5. **[Read]** Masterdata
6. **[Read]** QueueItem by `cbReceiptReference` and `Timestamp` (_join_)
7. `foreach` QueueItem
   1. **[Read]** QueueItem by `cbReceiptReference` and `ftQueueRow`
   2. **[Read]** ActionJournals by `Type`


## Echo operations
None.
