using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalFRRepository
    {
        Task<IEnumerable<ftJournalFR>> GetAsync();
        Task<ftJournalFR> GetAsync(Guid id);
    }
}
