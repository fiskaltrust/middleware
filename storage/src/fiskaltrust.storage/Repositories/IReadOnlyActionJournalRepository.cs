using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyActionJournalRepository
    {
        Task<IEnumerable<ftActionJournal>> GetAsync();
        Task<ftActionJournal> GetAsync(Guid id);
    }
}