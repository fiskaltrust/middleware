using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalITRepository : IReadOnlyJournalITRepository
    {
        Task InsertAsync(ftJournalIT journal);
    }
}
