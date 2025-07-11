using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReceiptJournalRepository : IReadOnlyReceiptJournalRepository
    {
        Task InsertAsync(ftReceiptJournal receiptJournal);
    }
}