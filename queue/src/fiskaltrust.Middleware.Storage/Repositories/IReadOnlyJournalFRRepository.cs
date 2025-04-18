using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.ES;

namespace fiskaltrust.Middleware.Storage.Repositories;

public interface IReadOnlyJournalESRepository
{
    Task<IEnumerable<ftJournalES>> GetAsync();

    Task<ftJournalES> GetAsync(Guid id);
}