using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyJournalESRepository
    {
        Task<IEnumerable<ftJournalES>> GetAsync();
        Task<ftJournalES> GetAsync(Guid id);
    }
}
