using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalDERepository : IReadOnlyJournalDERepository
    {
        Task InsertAsync(ftJournalDE journal);
    }
}
