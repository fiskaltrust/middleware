﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.storage.V0
{
    public interface IReadOnlyReceiptJournalRepository
    {
        Task<IEnumerable<ftReceiptJournal>> GetAsync();
        Task<ftReceiptJournal> GetAsync(Guid id);
    }
}