using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.MySQL.Repositories.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.MySQL.Repositories
{
    public class MySQLConfigurationRepository : IConfigurationRepository
    {
        private readonly MySQLCashBoxRepository _cashBoxRepository;
        private readonly MySQLQueueRepository _queueRepository;
        private readonly MySQLQueueATRepository _queueATRepository;
        private readonly MySQLQueueDERepository _queueDERepository;
        private readonly MySQLQueueFRRepository _queueFRRepository;
        private readonly MySQLQueueITRepository _queueITRepository;
        private readonly MySQLQueueMERepository _queueMERepository;
        private readonly MySQLSignaturCreationUnitATRepository _signaturCreationUnitATRepository;
        private readonly MySQLSignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly MySQLSignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly MySQLSignaturCreationUnitITRepository _signaturCreationUnitITRepository;
        private readonly MySQLSignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public MySQLConfigurationRepository() { }

        public MySQLConfigurationRepository(string connectionString)
        {
            _cashBoxRepository = new MySQLCashBoxRepository(connectionString);
            _queueRepository = new MySQLQueueRepository(connectionString);
            _queueATRepository = new MySQLQueueATRepository(connectionString);
            _queueDERepository = new MySQLQueueDERepository(connectionString);
            _queueFRRepository = new MySQLQueueFRRepository(connectionString);
            _queueITRepository = new MySQLQueueITRepository(connectionString);
            _queueMERepository = new MySQLQueueMERepository(connectionString);
            _signaturCreationUnitATRepository = new MySQLSignaturCreationUnitATRepository(connectionString);
            _signaturCreationUnitDERepository = new MySQLSignaturCreationUnitDERepository(connectionString);
            _signaturCreationUnitFRRepository = new MySQLSignaturCreationUnitFRRepository(connectionString);
            _signaturCreationUnitITRepository = new MySQLSignaturCreationUnitITRepository(connectionString);
            _signaturCreationUnitMERepository = new MySQLSignaturCreationUnitMERepository(connectionString);
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
     
        public async Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitMEId) => await _signaturCreationUnitMERepository.GetAsync(signaturCreationUnitMEId).ConfigureAwait(false);

        public async Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => await _signaturCreationUnitMERepository.GetAsync().ConfigureAwait(false);
        
        public async Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => await _signaturCreationUnitMERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

    }
}
