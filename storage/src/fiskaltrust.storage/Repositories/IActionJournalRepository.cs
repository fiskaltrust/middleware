using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IActionJournalRepository : IReadOnlyActionJournalRepository
    {
        Task InsertAsync(ftActionJournal actionJournal);
    }
}