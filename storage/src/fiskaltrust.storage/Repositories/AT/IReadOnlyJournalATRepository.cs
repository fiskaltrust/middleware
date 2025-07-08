using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalATRepository
    {
        Task<IEnumerable<ftJournalAT>> GetAsync();
        Task<ftJournalAT> GetAsync(Guid id);
    }
}
