using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.ES;

namespace fiskaltrust.Middleware.Storage.Repositories;

public interface IJournalESRepository : IReadOnlyJournalESRepository
{
    Task InsertAsync(ftJournalES journal);
}