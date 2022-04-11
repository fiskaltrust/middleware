using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories
{
    public class EFCoreConfigurationRepository : IConfigurationRepository
    {
        private readonly AbstractEFCoreRepostiory<Guid, ftCashBox> _cashBoxRepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftQueue> _queueRepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftQueueAT> _queueATRepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftQueueDE> _queueDERepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftQueueME> _queueMERepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftQueueFR> _queueFRRepository;
        private readonly AbstractEFCoreRepostiory<Guid, ftSignaturCreationUnitAT> _signaturCreationUnitATRepository;
        private readonly EFCoreSignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly EFCoreSignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly EFCoreSignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public EFCoreConfigurationRepository() { }

        public EFCoreConfigurationRepository(MiddlewareDbContext dbContext) 
        {
            _cashBoxRepository = new EFCoreCashBoxRepository(dbContext);
            _queueRepository = new EFCoreQueueRepository(dbContext);
            _queueATRepository = new EFCoreQueueATRepository(dbContext);
            _queueDERepository = new EFCoreQueueDERepository(dbContext);
            _queueFRRepository = new EFCoreQueueFRRepository(dbContext);
            _queueMERepository = new EFCoreQueueMERepository(dbContext);
            _signaturCreationUnitATRepository = new EFCoreSignaturCreationUnitATRepository(dbContext);
            _signaturCreationUnitDERepository = new EFCoreSignaturCreationUnitDERepository(dbContext);
            _signaturCreationUnitFRRepository = new EFCoreSignaturCreationUnitFRRepository(dbContext);
            _signaturCreationUnitMERepository = new EFCoreSignaturCreationUnitMERepository(dbContext);
        }

        public async Task<ftCashBox> GetCashBoxAsync(Guid cashBoxId) => await _cashBoxRepository.GetAsync(cashBoxId);
        public async Task<IEnumerable<ftCashBox>> GetCashBoxListAsync() => await _cashBoxRepository.GetAsync();
        public async Task InsertOrUpdateCashBoxAsync(ftCashBox cashBox) => await _cashBoxRepository.InsertOrUpdateAsync(cashBox);

        public async Task<ftQueue> GetQueueAsync(Guid id) => await _queueRepository.GetAsync(id);
        public async Task<IEnumerable<ftQueue>> GetQueueListAsync() => await _queueRepository.GetAsync();
        public async Task InsertOrUpdateQueueAsync(ftQueue queue) => await _queueRepository.InsertOrUpdateAsync(queue);

        public async Task<ftQueueAT> GetQueueATAsync(Guid id) => await _queueATRepository.GetAsync(id);
        public async Task<IEnumerable<ftQueueAT>> GetQueueATListAsync() => await _queueATRepository.GetAsync();
        public async Task InsertOrUpdateQueueATAsync(ftQueueAT queueAT) => await _queueATRepository.InsertOrUpdateAsync(queueAT);

        public async Task<ftQueueDE> GetQueueDEAsync(Guid id) => await _queueDERepository.GetAsync(id);
        public async Task<IEnumerable<ftQueueDE>> GetQueueDEListAsync() => await _queueDERepository.GetAsync();
        public async Task InsertOrUpdateQueueDEAsync(ftQueueDE queueDE) => await _queueDERepository.InsertOrUpdateAsync(queueDE);

        public async Task<ftQueueFR> GetQueueFRAsync(Guid id) => await _queueFRRepository.GetAsync(id);
        public async Task<IEnumerable<ftQueueFR>> GetQueueFRListAsync() => await _queueFRRepository.GetAsync();
        public async Task InsertOrUpdateQueueFRAsync(ftQueueFR queueFR) => await _queueFRRepository.InsertOrUpdateAsync(queueFR);

        public async Task InsertOrUpdateQueueMEAsync(ftQueueME queue) => await _queueMERepository.InsertOrUpdateAsync(queue);   

        public async Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid id) => await _signaturCreationUnitATRepository.GetAsync(id);
        public async Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync() => await _signaturCreationUnitATRepository.GetAsync();
        public async Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu) => await _signaturCreationUnitATRepository.InsertOrUpdateAsync(scu);

        public async Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid id) => await _signaturCreationUnitDERepository.GetAsync(id);
        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync();
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu);

        public async Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid id) => await _signaturCreationUnitFRRepository.GetAsync(id);
        public async Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync() => await _signaturCreationUnitFRRepository.GetAsync();
        public async Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu) => await _signaturCreationUnitFRRepository.InsertOrUpdateAsync(scu);

        public async Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => await _signaturCreationUnitMERepository.InsertOrUpdateAsync(scu);
        public Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => throw new NotImplementedException();
        public Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitDEId) => throw new NotImplementedException();
        public Task<IEnumerable<ftQueueME>> GetQueueMEListAsync() => throw new NotImplementedException();
        public Task<ftQueueME> GetQueueMEAsync(Guid queueMEId) => throw new NotImplementedException();
    }
}