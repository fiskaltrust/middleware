using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalMERepository
    {
        Task<IEnumerable<ftJournalME>> GetAsync();
        Task<ftJournalME> GetAsync(Guid id);
        Task<ftJournalME> GetLastEntryAsync();
    }
}
