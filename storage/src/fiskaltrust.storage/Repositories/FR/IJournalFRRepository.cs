using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalFRRepository : IReadOnlyJournalFRRepository
    {
        Task InsertAsync(ftJournalFR journal);
    }
}
