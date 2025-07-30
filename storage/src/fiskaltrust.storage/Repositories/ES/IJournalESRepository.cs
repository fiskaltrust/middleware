using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalESRepository : IReadOnlyJournalESRepository
    {
        Task InsertAsync(ftJournalES journal);
    }
}
