using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalMERepository : IReadOnlyJournalMERepository
    {
        Task InsertAsync(ftJournalME journal);
    }
}
