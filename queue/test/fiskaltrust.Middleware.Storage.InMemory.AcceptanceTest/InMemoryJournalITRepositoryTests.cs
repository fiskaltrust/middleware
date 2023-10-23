﻿using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.IT;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryJournalITRepositoryTests : AbstractJournalITRepositoryTests
    {
        public override Task<IReadOnlyJournalITRepository> CreateReadOnlyRepository(IEnumerable<ftJournalIT> entries) => Task.FromResult<IReadOnlyJournalITRepository>(new InMemoryJournalITRepository(entries));

        public override Task<IMiddlewareJournalITRepository> CreateRepository(IEnumerable<ftJournalIT> entries) => Task.FromResult<IMiddlewareJournalITRepository>(new InMemoryJournalITRepository(entries));
    }
}
