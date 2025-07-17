using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IJournalATRepository : IReadOnlyJournalATRepository
    {
        Task InsertAsync(ftJournalAT journal);
    }
}
