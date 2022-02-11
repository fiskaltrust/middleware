using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories
{
    public class InMemoryConfigurationRepository : IConfigurationRepository
    {
        private readonly AbstractInMemoryRepository<Guid, ftCashBox> _cashBoxRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueue> _queueRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueAT> _queueATRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueDE> _queueDERepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueFR> _queueFRRepository;
        private readonly AbstractInMemoryRepository<Guid, ftSignaturCreationUnitAT> _signaturCreationUnitATRepository;
        private readonly InMemorySignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly InMemorySignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;

        public InMemoryConfigurationRepository() 
        {
            _cashBoxRepository = new InMemoryCashBoxRepository();
            _queueRepository = new InMemoryQueueRepository();
            _queueATRepository = new InMemoryQueueATRepository();
            _queueDERepository = new InMemoryQueueDERepository();
            _queueFRRepository = new InMemoryQueueFRRepository();
            _signaturCreationUnitATRepository = new InMemorySignaturCreationUnitATRepository();
            _signaturCreationUnitDERepository = new InMemorySignaturCreationUnitDERepository();
            _signaturCreationUnitFRRepository = new InMemorySignaturCreationUnitFRRepository();
        }

        public InMemoryConfigurationRepository(IEnumerable<ftCashBox> cashBoxes = null,
            IEnumerable<ftQueue> queues = null,
            IEnumerable<ftQueueAT> queuesAT = null,
            IEnumerable<ftQueueDE> queuesDE = null,
            IEnumerable<ftQueueFR> queuesFR = null,
            IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null,
            IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null,
            IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null)
        {
            _cashBoxRepository = new InMemoryCashBoxRepository(cashBoxes);
            _queueRepository = new InMemoryQueueRepository(queues);
            _queueATRepository = new InMemoryQueueATRepository(queuesAT);
            _queueDERepository = new InMemoryQueueDERepository(queuesDE);
            _queueFRRepository = new InMemoryQueueFRRepository(queuesFR);
            _signaturCreationUnitATRepository = new InMemorySignaturCreationUnitATRepository(signatureCreateUnitsAT);
            _signaturCreationUnitDERepository = new InMemorySignaturCreationUnitDERepository(signatureCreateUnitsDE);
            _signaturCreationUnitFRRepository = new InMemorySignaturCreationUnitFRRepository(signatureCreateUnitsFR);
        }

        public async Task<ftCashBox> GetCashBoxAsync(Guid cashBoxId) => await _cashBoxRepository.GetAsync(cashBoxId).ConfigureAwait(false);
        public async Task<IEnumerable<ftCashBox>> GetCashBoxListAsync() => await _cashBoxRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateCashBoxAsync(ftCashBox cashBox) => await _cashBoxRepository.InsertOrUpdateAsync(cashBox).ConfigureAwait(false);

        public async Task<ftQueue> GetQueueAsync(Guid id) => await _queueRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueue>> GetQueueListAsync() => await _queueRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueAsync(ftQueue queue) => await _queueRepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);

        public async Task<ftQueueAT> GetQueueATAsync(Guid id) => await _queueATRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueAT>> GetQueueATListAsync() => await _queueATRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueATAsync(ftQueueAT queueAT) => await _queueATRepository.InsertOrUpdateAsync(queueAT).ConfigureAwait(false);

        public async Task<ftQueueDE> GetQueueDEAsync(Guid id) => await _queueDERepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueDE>> GetQueueDEListAsync() => await _queueDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueDEAsync(ftQueueDE queueDE) => await _queueDERepository.InsertOrUpdateAsync(queueDE).ConfigureAwait(false);

        public async Task<ftQueueFR> GetQueueFRAsync(Guid id) => await _queueFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueFR>> GetQueueFRListAsync() => await _queueFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueFRAsync(ftQueueFR queueFR) => await _queueFRRepository.InsertOrUpdateAsync(queueFR).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid id) => await _signaturCreationUnitATRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync() => await _signaturCreationUnitATRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu) => await _signaturCreationUnitATRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid id) => await _signaturCreationUnitDERepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid id) => await _signaturCreationUnitFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync() => await _signaturCreationUnitFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu) => await _signaturCreationUnitFRRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
    }
}