using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.Azure.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public class AzureConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        public override async Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
            => await CreateRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, signatureCreateUnitsFR, signatureCreateUnitsME);

        public override async Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            var azureConfigurationRepository = new AzureConfigurationRepository(Guid.NewGuid(), Constants.AzureStorageConnectionString);

            foreach (var entry in cashBoxes ?? new List<ftCashBox>())
            {
                await azureConfigurationRepository.InsertOrUpdateCashBoxAsync(entry);
            }

            foreach (var entry in queues ?? new List<ftQueue>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueAsync(entry);
            }

            foreach (var entry in queuesAT ?? new List<ftQueueAT>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueATAsync(entry);
            }

            foreach (var entry in queuesDE ?? new List<ftQueueDE>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueDEAsync(entry);
            }

            foreach (var entry in queuesME ?? new List<ftQueueME>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueMEAsync(entry);
            }

            foreach (var entry in queuesFR ?? new List<ftQueueFR>())
            {
                await azureConfigurationRepository.InsertOrUpdateQueueFRAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsAT ?? new List<ftSignaturCreationUnitAT>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitATAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsDE ?? new List<ftSignaturCreationUnitDE>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitDEAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsFR ?? new List<ftSignaturCreationUnitFR>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitFRAsync(entry);
            }

            foreach (var entry in signatureCreateUnitsME ?? new List<ftSignaturCreationUnitME>())
            {
                await azureConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(entry);
            }

            return azureConfigurationRepository;
        }
    }
}
