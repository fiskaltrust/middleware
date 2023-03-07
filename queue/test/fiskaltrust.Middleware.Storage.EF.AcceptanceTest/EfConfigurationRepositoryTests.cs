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

        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueES> queuesES = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitES> signatureCreateUnitsES = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
            => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesES, queuesFR, queuesIT, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsES, signatureCreateUnitsFR, signatureCreateUnitsIT, signatureCreateUnitsME);

        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueES> queuesES = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitES> signatureCreateUnitsES = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
            => await CreateConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesES, queuesFR, queuesIT, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE,signatureCreateUnitsES, signatureCreateUnitsFR,signatureCreateUnitsIT, signatureCreateUnitsME);

        private async Task<EfConfigurationRepository> CreateConfigurationRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueES> queuesES = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitES> signatureCreateUnitsES = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            var queueId = Guid.NewGuid();
            var repository = new EfConfigurationRepository(new MiddlewareDbContext(EfConnectionStringFixture.DatabaseConnectionString, queueId));
            EfStorageBootstrapper.Update(EfConnectionStringFixture.DatabaseConnectionString, 30 * 60, queueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());

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

            foreach (var item in queuesES ?? new List<ftQueueES>())
            {
                await repository.InsertOrUpdateQueueESAsync(item);
            }

            foreach (var item in queuesFR ?? new List<ftQueueFR>())
            {
                await repository.InsertOrUpdateQueueFRAsync(item);
            }

            foreach (var item in queuesIT ?? new List<ftQueueIT>())
            {
                await repository.InsertOrUpdateQueueITAsync(item);
            }

            foreach (var item in queuesME ?? new List<ftQueueME>())
            {
                await repository.InsertOrUpdateQueueMEAsync(item);
            }

            foreach (var item in signatureCreateUnitsAT ?? new List<ftSignaturCreationUnitAT>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitATAsync(item);
            }

            foreach (var item in signatureCreateUnitsDE ?? new List<ftSignaturCreationUnitDE>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitDEAsync(item);
            }

            foreach (var item in signatureCreateUnitsES ?? new List<ftSignaturCreationUnitES>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitESAsync(item);
            }

            foreach (var item in signatureCreateUnitsFR ?? new List<ftSignaturCreationUnitFR>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitFRAsync(item);
            }

            foreach (var item in signatureCreateUnitsIT ?? new List<ftSignaturCreationUnitIT>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitITAsync(item);
            }

            foreach (var item in signatureCreateUnitsME ?? new List<ftSignaturCreationUnitME>())
            {
                await repository.InsertOrUpdateSignaturCreationUnitMEAsync(item);
            }

            return repository;
        }
    }
}
