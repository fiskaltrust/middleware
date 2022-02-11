// using System;
// using System.Collections.Generic;
// using fiskaltrust.Middleware.Abstractions;
// using fiskaltrust.Middleware.Storage.AcceptanceTest;
// using fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.SQLServer.Fixtures;
// using fiskaltrust.Middleware.Storage.EFCore.Repositories;
// using fiskaltrust.Middleware.Storage.EFCore.SQLServer;
// using fiskaltrust.storage.V0;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Moq;
// using Xunit;

// namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.SQLServer
// {
//     [Collection(EFCoreSqlServerStorageCollectionFixture.CollectionName)]
//     public class EFCoreSQLServerConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
//     {

//         public override IReadOnlyConfigurationRepository CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
//             => CreateConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR);


//         public override IConfigurationRepository CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
//             => CreateConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR);


//         private EFCoreConfigurationRepository CreateConfigurationRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
//         {
//             var optionsBuilder = new DbContextOptionsBuilder<SQLServerMiddlewareDbContext>();
//             optionsBuilder.UseSqlServer(EFCoreSqlServerConnectionStringFixture.DatabaseConnectionString);

//             var queueId = Guid.NewGuid();
//             var repository = new EFCoreConfigurationRepository(new SQLServerMiddlewareDbContext(optionsBuilder.Options, queueId));
//             EFCoreSQLServerStorageBootstrapper.Update(optionsBuilder.Options, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

//             foreach (var item in cashBoxes ?? new List<ftCashBox>())
//             {
//                 repository.InsertOrUpdateCashBoxAsync(item).Wait();
//             }

//             foreach (var item in queues ?? new List<ftQueue>())
//             {
//                 repository.InsertOrUpdateQueueAsync(item).Wait();
//             }

//             foreach (var item in queuesAT ?? new List<ftQueueAT>())
//             {
//                 repository.InsertOrUpdateQueueATAsync(item).Wait();
//             }

//             foreach (var item in queuesDE ?? new List<ftQueueDE>())
//             {
//                 repository.InsertOrUpdateQueueDEAsync(item).Wait();
//             }

//             foreach (var item in queuesFR ?? new List<ftQueueFR>())
//             {
//                 repository.InsertOrUpdateQueueFRAsync(item).Wait();
//             }

//             foreach (var item in signatureCreateUnitsAT ?? new List<ftSignaturCreationUnitAT>())
//             {
//                 repository.InsertOrUpdateSignaturCreationUnitATAsync(item).Wait();
//             }

//             foreach (var item in signatureCreateUnitsDE ?? new List<ftSignaturCreationUnitDE>())
//             {
//                 repository.InsertOrUpdateSignaturCreationUnitDEAsync(item).Wait();
//             }

//             foreach (var item in signatureCreateUnitsFR ?? new List<ftSignaturCreationUnitFR>())
//             {
//                 repository.InsertOrUpdateSignaturCreationUnitFRAsync(item).Wait();
//             }

//             return repository;
//         }
//     }
// }
