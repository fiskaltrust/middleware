using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories.DE;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfFailedStartTransactionRepositoryTests : AbstractFailedStartTransactionRepositoryTests
    {
        public override Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateReadOnlyRepository(IEnumerable<FailedStartTransaction> entries) => CreateRepository(entries);

        public override async Task<IPersistentTransactionRepository<FailedStartTransaction>> CreateRepository(IEnumerable<FailedStartTransaction> entries)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfFailedStartTransactionRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId, 60));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
            
            foreach (var item in entries)
            {
                await repository.InsertAsync(item);
            }

            return repository;
        }
    }
}
