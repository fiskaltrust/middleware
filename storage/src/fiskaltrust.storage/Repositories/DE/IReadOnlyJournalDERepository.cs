using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalDERepository
    {
        Task<IEnumerable<ftJournalDE>> GetAsync();
        Task<ftJournalDE> GetAsync(Guid id);
    }
}
