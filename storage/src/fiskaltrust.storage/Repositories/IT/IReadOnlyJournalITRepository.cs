using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalITRepository
    {
        Task<IEnumerable<ftJournalIT>> GetAsync();
        Task<ftJournalIT> GetAsync(Guid id);
    }
}
