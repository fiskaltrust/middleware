// using System;
// using System.Collections.Generic;
// using fiskaltrust.Middleware.Abstractions;
// using fiskaltrust.Middleware.Storage.AcceptanceTest;
// using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.SQLServer.Fixtures;
// using fiskaltrust.Middleware.Storage.EFCore.Repositories.DE;
// using fiskaltrust.Middleware.Storage.EFCore.SQLServer;
// using fiskaltrust.storage.V0;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit;

// namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.SQLServer
// {
//     [Collection(EFCoreSqlServerStorageCollectionFixture.CollectionName)]
//     public class EFCoreSQLServerJournalDERepositoryTests : AbstractJournalDERepositoryTests
//     {
//         public override IReadOnlyJournalDERepository CreateReadOnlyRepository(IEnumerable<ftJournalDE> entries) => CreateRepository(entries);

//         public override IJournalDERepository CreateRepository(IEnumerable<ftJournalDE> entries)
//         {
//             var optionsBuilder = new DbContextOptionsBuilder<SQLServerMiddlewareDbContext>();
//             optionsBuilder.UseSqlServer(EFCoreSqlServerConnectionStringFixture.DatabaseConnectionString);

//             var queueId = Guid.NewGuid();
//             var repository = new EFCoreJournalDERepository(new SQLServerMiddlewareDbContext(optionsBuilder.Options, queueId));
//             EFCoreSQLServerStorageBootstrapper.Update(optionsBuilder.Options, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

//             foreach (var item in entries)
//             {
//                 repository.InsertAsync(item).Wait();
//             }

//             return repository;
//         }
//     }
// }
