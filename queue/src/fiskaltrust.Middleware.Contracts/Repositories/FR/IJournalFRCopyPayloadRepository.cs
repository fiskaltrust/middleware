namespace fiskaltrust.Middleware.Contracts.Repositories.FR
{
    public interface IJournalFRCopyPayloadRepository
    {
        int JournalFRGetCountOfCopies(string cbPreviousReceiptReference);

        bool InsertJournalFRCopyPayload(ftJournalFRCopyPayload c);

        bool HasJournalFRCopyPayloads();
    }
}