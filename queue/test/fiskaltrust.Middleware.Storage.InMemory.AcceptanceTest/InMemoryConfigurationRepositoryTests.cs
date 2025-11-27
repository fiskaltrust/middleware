using fiskaltrust.Middleware.Storage.AcceptanceTest;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.InMemory.AcceptanceTest
{
    public class InMemoryConfigurationRepositoryTests : AbstractConfigurationRepositoryTests
    {
        public override Task<IReadOnlyConfigurationRepository> CreateReadOnlyRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        => Task.FromResult<IReadOnlyConfigurationRepository>(new InMemoryConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesIT, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, null, signatureCreateUnitsFR, signatureCreateUnitsIT, signatureCreateUnitsME));
        public override Task<IConfigurationRepository> CreateRepository(IEnumerable<ftCashBox> cashBoxes = null, IEnumerable<ftQueue> queues = null, IEnumerable<ftQueueAT> queuesAT = null, IEnumerable<ftQueueDE> queuesDE = null, IEnumerable<ftQueueFR> queuesFR = null, IEnumerable<ftQueueIT> queuesIT = null, IEnumerable<ftQueueME> queuesME = null, IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null, IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null, IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null, IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null, IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        => Task.FromResult<IConfigurationRepository>(new InMemoryConfigurationRepository(cashBoxes, queues, queuesAT, queuesDE, queuesFR, queuesIT, queuesME, signatureCreateUnitsAT, signatureCreateUnitsDE, null, signatureCreateUnitsFR, signatureCreateUnitsIT, signatureCreateUnitsME));
    }
}
