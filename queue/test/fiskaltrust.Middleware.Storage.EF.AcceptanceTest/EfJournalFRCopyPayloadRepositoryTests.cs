using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories.FR;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfJournalFRCopyPayloadRepositoryAcceptanceTests : AbstractJournalFRCopyPayloadRepositoryTests
    {
        public override async Task<IJournalFRCopyPayloadRepository> CreateRepository(IEnumerable<ftJournalFRCopyPayload> entries)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfJournalFRCopyPayloadRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId, 60));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            foreach (var item in entries)
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }
    }
}
