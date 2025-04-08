using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.EF.Repositories.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories
{
    public class EfConfigurationRepository : IConfigurationRepository
    {
        private readonly AbstractEFRepostiory<Guid, ftCashBox> _cashBoxRepository;
        private readonly AbstractEFRepostiory<Guid, ftQueue> _queueRepository;
        private readonly AbstractEFRepostiory<Guid, ftQueueAT> _queueATRepository;
        private readonly AbstractEFRepostiory<Guid, ftQueueDE> _queueDERepository;
        private readonly AbstractEFRepostiory<Guid, ftQueueFR> _queueFRRepository;
        private readonly AbstractEFRepostiory<Guid, ftQueueIT> _queueITRepository;
        private readonly AbstractEFRepostiory<Guid, ftQueueME> _queueMERepository;
        private readonly AbstractEFRepostiory<Guid, ftSignaturCreationUnitAT> _signaturCreationUnitATRepository;
        private readonly EfSignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly EfSignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly EfSignaturCreationUnitITRepository _signaturCreationUnitITRepository;
        private readonly EfSignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public EfConfigurationRepository() { }

        public EfConfigurationRepository(MiddlewareDbContext dbContext)
        {
            _cashBoxRepository = new EfCashBoxRepository(dbContext);
            _queueRepository = new EfQueueRepository(dbContext);
            _queueATRepository = new EfQueueATRepository(dbContext);
            _queueDERepository = new EfQueueDERepository(dbContext);
            _queueFRRepository = new EfQueueFRRepository(dbContext);
            _queueITRepository = new EfQueueITRepository(dbContext);
            _queueMERepository = new EfQueueMERepository(dbContext);
            _signaturCreationUnitATRepository = new EfSignaturCreationUnitATRepository(dbContext);
            _signaturCreationUnitDERepository = new EfSignaturCreationUnitDERepository(dbContext);
            _signaturCreationUnitFRRepository = new EfSignaturCreationUnitFRRepository(dbContext);
            _signaturCreationUnitITRepository = new EfSignaturCreationUnitITRepository(dbContext);
            _signaturCreationUnitMERepository = new EfSignaturCreationUnitMERepository(dbContext);
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
        public Task<IEnumerable<ftQueueES>> GetQueueESListAsync() => throw new NotImplementedException();

        public Task<ftQueueES> GetQueueESAsync(Guid queueESId) => throw new NotImplementedException();

        public async Task<IEnumerable<ftQueueDE>> GetQueueDEListAsync() => await _queueDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueDEAsync(ftQueueDE queueDE) => await _queueDERepository.InsertOrUpdateAsync(queueDE).ConfigureAwait(false);
        public Task InsertOrUpdateQueueESAsync(ftQueueES queue) => throw new NotImplementedException();

        public async Task<ftQueueFR> GetQueueFRAsync(Guid id) => await _queueFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueFR>> GetQueueFRListAsync() => await _queueFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueFRAsync(ftQueueFR queueFR) => await _queueFRRepository.InsertOrUpdateAsync(queueFR).ConfigureAwait(false);

        public async Task<ftQueueIT> GetQueueITAsync(Guid id) => await _queueITRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueIT>> GetQueueITListAsync() => await _queueITRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueITAsync(ftQueueIT queueIT) => await _queueITRepository.InsertOrUpdateAsync(queueIT).ConfigureAwait(false);

        public async Task<ftQueueME> GetQueueMEAsync(Guid queueMEId) => await _queueMERepository.GetAsync(queueMEId).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueME>> GetQueueMEListAsync() => await _queueMERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueMEAsync(ftQueueME queue) => await _queueMERepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid id) => await _signaturCreationUnitATRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync() => await _signaturCreationUnitATRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu) => await _signaturCreationUnitATRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid id) => await _signaturCreationUnitDERepository.GetAsync(id).ConfigureAwait(false);
        public Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync() => throw new NotImplementedException();

        public Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid signaturCreationUnitESId) => throw new NotImplementedException();

        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu) => throw new NotImplementedException();

        public async Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid id) => await _signaturCreationUnitFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync() => await _signaturCreationUnitFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu) => await _signaturCreationUnitFRRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitIT> GetSignaturCreationUnitITAsync(Guid id) => await _signaturCreationUnitITRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitIT>> GetSignaturCreationUnitITListAsync() => await _signaturCreationUnitITRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitITAsync(ftSignaturCreationUnitIT scu) => await _signaturCreationUnitITRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitDEId) => await _signaturCreationUnitMERepository.GetAsync(signaturCreationUnitDEId).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => await _signaturCreationUnitMERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => await _signaturCreationUnitMERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
  }
}