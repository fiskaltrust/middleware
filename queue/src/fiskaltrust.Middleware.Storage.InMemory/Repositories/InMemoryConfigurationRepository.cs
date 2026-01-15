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
        private readonly AbstractInMemoryRepository<Guid, ftQueueES> _queueESRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueFR> _queueFRRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueIT> _queueITRepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueME> _queueMERepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueBE> _queueBERepository;
        private readonly AbstractInMemoryRepository<Guid, ftQueueGR> _queueGRRepository;
        private readonly AbstractInMemoryRepository<Guid, ftSignaturCreationUnitAT> _signaturCreationUnitATRepository;
        private readonly AbstractInMemoryRepository<Guid, ftSignaturCreationUnitBE> _signaturCreationUnitBERepository;
        private readonly AbstractInMemoryRepository<Guid, ftSignaturCreationUnitGR> _signaturCreationUnitGRRepository;
        private readonly InMemorySignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly InMemorySignaturCreationUnitESRepository _signaturCreationUnitESRepository;
        private readonly InMemorySignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly InMemorySignaturCreationUnitITRepository _signaturCreationUnitITRepository;
        private readonly InMemorySignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public InMemoryConfigurationRepository()
        {
            _cashBoxRepository = new InMemoryCashBoxRepository();
            _queueRepository = new InMemoryQueueRepository();
            _queueATRepository = new InMemoryQueueATRepository();
            _queueDERepository = new InMemoryQueueDERepository();
            _queueESRepository = new InMemoryQueueESRepository();
            _queueFRRepository = new InMemoryQueueFRRepository();
            _queueITRepository = new InMemoryQueueITRepository();
            _queueMERepository = new InMemoryQueueMERepository();
            _queueBERepository = new InMemoryQueueBERepository();
            _queueGRRepository = new InMemoryQueueGRRepository();
            _signaturCreationUnitATRepository = new InMemorySignaturCreationUnitATRepository();
            _signaturCreationUnitBERepository = new InMemorySignaturCreationUnitBERepository();
            _signaturCreationUnitGRRepository = new InMemorySignaturCreationUnitGRRepository();
            _signaturCreationUnitDERepository = new InMemorySignaturCreationUnitDERepository();
            _signaturCreationUnitESRepository = new InMemorySignaturCreationUnitESRepository();
            _signaturCreationUnitFRRepository = new InMemorySignaturCreationUnitFRRepository();
            _signaturCreationUnitITRepository = new InMemorySignaturCreationUnitITRepository();
            _signaturCreationUnitMERepository = new InMemorySignaturCreationUnitMERepository();
        }

        public InMemoryConfigurationRepository(IEnumerable<ftCashBox> cashBoxes = null,
            IEnumerable<ftQueue> queues = null,
            IEnumerable<ftQueueAT> queuesAT = null,
            IEnumerable<ftQueueDE> queuesDE = null,
            IEnumerable<ftQueueFR> queuesFR = null,
            IEnumerable<ftQueueIT> queuesIT = null,
            IEnumerable<ftQueueME> queuesME = null,
            IEnumerable<ftSignaturCreationUnitAT> signatureCreateUnitsAT = null,
            IEnumerable<ftSignaturCreationUnitDE> signatureCreateUnitsDE = null,
            IEnumerable<ftSignaturCreationUnitFR> signatureCreateUnitsFR = null,
            IEnumerable<ftSignaturCreationUnitIT> signatureCreateUnitsIT = null,
            IEnumerable<ftSignaturCreationUnitME> signatureCreateUnitsME = null)
        {
            _cashBoxRepository = new InMemoryCashBoxRepository(cashBoxes);
            _queueRepository = new InMemoryQueueRepository(queues);
            _queueATRepository = new InMemoryQueueATRepository(queuesAT);
            _queueDERepository = new InMemoryQueueDERepository(queuesDE);
            _queueFRRepository = new InMemoryQueueFRRepository(queuesFR);
            _queueITRepository = new InMemoryQueueITRepository(queuesIT);
            _queueMERepository = new InMemoryQueueMERepository(queuesME);
            _signaturCreationUnitATRepository = new InMemorySignaturCreationUnitATRepository(signatureCreateUnitsAT);
            _signaturCreationUnitDERepository = new InMemorySignaturCreationUnitDERepository(signatureCreateUnitsDE);
            _signaturCreationUnitFRRepository = new InMemorySignaturCreationUnitFRRepository(signatureCreateUnitsFR);
            _signaturCreationUnitITRepository = new InMemorySignaturCreationUnitITRepository(signatureCreateUnitsIT);
            _signaturCreationUnitMERepository = new InMemorySignaturCreationUnitMERepository(signatureCreateUnitsME);
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

        public async Task<ftQueueIT> GetQueueITAsync(Guid id) => await _queueITRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueIT>> GetQueueITListAsync() => await _queueITRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueITAsync(ftQueueIT queueIT) => await _queueITRepository.InsertOrUpdateAsync(queueIT).ConfigureAwait(false);

        public async Task InsertOrUpdateQueueMEAsync(ftQueueME queue) => await _queueMERepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueME>> GetQueueMEListAsync() => await _queueMERepository.GetAsync().ConfigureAwait(false);
        public async Task<ftQueueME> GetQueueMEAsync(Guid queueMEId) => await _queueMERepository.GetAsync(queueMEId).ConfigureAwait(false);

        public async Task InsertOrUpdateQueueBEAsync(ftQueueBE queue) => await _queueBERepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueBE>> GetQueueBEListAsync() => await _queueBERepository.GetAsync().ConfigureAwait(false);
        public async Task<ftQueueBE> GetQueueBEAsync(Guid queueBEId) => await _queueBERepository.GetAsync(queueBEId).ConfigureAwait(false);

        public async Task InsertOrUpdateQueueGRAsync(ftQueueGR queue) => await _queueGRRepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueGR>> GetQueueGRListAsync() => await _queueGRRepository.GetAsync().ConfigureAwait(false);
        public async Task<ftQueueGR> GetQueueGRAsync(Guid queueGRId) => await _queueGRRepository.GetAsync(queueGRId).ConfigureAwait(false);

        public Task<IEnumerable<ftQueueES>> GetQueueESListAsync() => _queueESRepository.GetAsync();
        public Task<ftQueueES> GetQueueESAsync(Guid queueESId) => _queueESRepository.GetAsync(queueESId);
        public Task InsertOrUpdateQueueESAsync(ftQueueES queue) => _queueESRepository.InsertOrUpdateAsync(queue);

        public async Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid id) => await _signaturCreationUnitATRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync() => await _signaturCreationUnitATRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu) => await _signaturCreationUnitATRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid id) => await _signaturCreationUnitDERepository.GetAsync(id).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId) => await _signaturCreationUnitESRepository.GetAsync(signaturCreationUnitESId).ConfigureAwait(false);

        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        Task IConfigurationRepository.InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu) => InsertOrUpdateSignaturCreationUnitESAsync(scu);

        public async Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid id) => await _signaturCreationUnitFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync() => await _signaturCreationUnitFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu) => await _signaturCreationUnitFRRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitIT> GetSignaturCreationUnitITAsync(Guid id) => await _signaturCreationUnitITRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitIT>> GetSignaturCreationUnitITListAsync() => await _signaturCreationUnitITRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitITAsync(ftSignaturCreationUnitIT scu) => await _signaturCreationUnitITRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => await _signaturCreationUnitMERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => await _signaturCreationUnitMERepository.GetAsync().ConfigureAwait(false);
        public async Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitDEId) => await _signaturCreationUnitMERepository.GetAsync(signaturCreationUnitDEId).ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitBEAsync(ftSignaturCreationUnitBE scu) => await _signaturCreationUnitBERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitBE>> GetSignaturCreationUnitBEListAsync() => await _signaturCreationUnitBERepository.GetAsync().ConfigureAwait(false);
        public async Task<ftSignaturCreationUnitBE> GetSignaturCreationUnitBEAsync(Guid signaturCreationUnitBEId) => await _signaturCreationUnitBERepository.GetAsync(signaturCreationUnitBEId).ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitGRAsync(ftSignaturCreationUnitGR scu) => await _signaturCreationUnitGRRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitGR>> GetSignaturCreationUnitGRListAsync() => await _signaturCreationUnitGRRepository.GetAsync().ConfigureAwait(false);
        public async Task<ftSignaturCreationUnitGR> GetSignaturCreationUnitGRAsync(Guid signaturCreationUnitGRId) => await _signaturCreationUnitGRRepository.GetAsync(signaturCreationUnitGRId).ConfigureAwait(false);

        public Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync() => _signaturCreationUnitESRepository.GetAsync();
        public async Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu) => await _signaturCreationUnitESRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public Task InsertOrUpdateQueueEUAsync(ftQueueEU queue) => throw new NotImplementedException();
        public Task<IEnumerable<ftQueueEU>> GetQueueEUListAsync() => throw new NotImplementedException();
        public Task<ftQueueEU> GetQueueEUAsync(Guid queueEUId) => throw new NotImplementedException();
    }
}