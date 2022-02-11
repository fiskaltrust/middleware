using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Base;
using fiskaltrust.Middleware.Storage.Ef;
using fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest
{
    [Collection(EfStorageCollectionFixture.CollectionName)]
    public class EfConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {

        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
            => await CreateConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR);


        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
            => await CreateConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR);


        private async Task<EfConfigurationRepository> CreateConfigurationRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfConfigurationRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

            foreach (var item in cashBoxes ?? new List<ftCashBox>())
            {
                await repository.InsertOrUpdateCashBoxAsync(item);
            }

            foreach (var item in queues ?? new List<ftQueue>())
            {
                await repository.InsertOrUpdateQueueAsync(item);
            }

            foreach (var item in queuesAT ?? new List<ftQueueAT>())
            {
                await repository.InsertOrUpdateQueueATAsync(item);
            }

            foreach (var item in queuesDE ?? new List<ftQueueDE>())
            {
                await repository.InsertOrUpdateQueueDEAsync(item);
            }

            foreach (var item in queuesFR ?? new List<ftQueueFR>())
            {
                await repository.InsertOrUpdateQueueFRAsync(item);
            }

            foreach (var item in signatureCreateUnitsAT ?? new List<ftSignaturCreationUnitAT>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitATAsync(item);
            }

            foreach (var item in signatureCreateUnitsDE ?? new List<ftSignaturCreationUnitDE>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitDEAsync(item);
            }

            foreach (var item in signatureCreateUnitsFR ?? new List<ftSignaturCreationUnitFR>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitFRAsync(item);
            }

            return repository;
        }
    }
}
