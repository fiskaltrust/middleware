using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.SQLite.Repositories.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.SQLite.Repositories
{
    public class SQLiteConfigurationRepository : IConfigurationRepository
    {
        private readonly SQLiteCashBoxRepository _cashBoxRepository;
        private readonly SQLiteQueueRepository _queueRepository;
        private readonly SQLiteQueueATRepository _queueATRepository;
        private readonly SQLiteQueueDERepository _queueDERepository;
        private readonly SQLiteQueueESRepository _queueESRepository;
        private readonly SQLiteQueueFRRepository _queueFRRepository;
        private readonly SQLiteQueueITRepository _queueITRepository;
        private readonly SQLiteQueueMERepository _queueMERepository;
        private readonly SQLiteSignaturCreationUnitATRepository _signaturCreationUnitATRepository;
        private readonly SQLiteSignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly SQLiteSignaturCreationUnitESRepository _signaturCreationUnitESRepository;
        private readonly SQLiteSignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly SQLiteSignaturCreationUnitITRepository _signaturCreationUnitITRepository;
        private readonly SQLiteSignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public SQLiteConfigurationRepository() { }

        public SQLiteConfigurationRepository(ISqliteConnectionFactory connectionFactory, string path)
        {
            _cashBoxRepository = new SQLiteCashBoxRepository(connectionFactory, path);
            _queueRepository = new SQLiteQueueRepository(connectionFactory, path);
            _queueATRepository = new SQLiteQueueATRepository(connectionFactory, path);
            _queueDERepository = new SQLiteQueueDERepository(connectionFactory, path);
            _queueESRepository = new SQLiteQueueESRepository(connectionFactory, path);
            _queueFRRepository = new SQLiteQueueFRRepository(connectionFactory, path);
            _queueITRepository = new SQLiteQueueITRepository(connectionFactory, path);
            _queueMERepository = new SQLiteQueueMERepository(connectionFactory, path);
            _signaturCreationUnitATRepository = new SQLiteSignaturCreationUnitATRepository(connectionFactory, path);
            _signaturCreationUnitDERepository = new SQLiteSignaturCreationUnitDERepository(connectionFactory, path);
            _signaturCreationUnitESRepository = new SQLiteSignaturCreationUnitESRepository(connectionFactory, path);
            _signaturCreationUnitFRRepository = new SQLiteSignaturCreationUnitFRRepository(connectionFactory, path);
            _signaturCreationUnitITRepository = new SQLiteSignaturCreationUnitITRepository(connectionFactory, path);
            _signaturCreationUnitMERepository = new SQLiteSignaturCreationUnitMERepository(connectionFactory, path);
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

        public async Task<ftQueueES> GetQueueESAsync(Guid id) => await _queueESRepository.GetAsync(id).ConfigureAwait(false);

        public async Task<IEnumerable<ftQueueES>> GetQueueESListAsync() => await _queueESRepository.GetAsync().ConfigureAwait(false);

        public async Task InsertOrUpdateQueueESAsync(ftQueueES queueES) => await _queueESRepository.InsertOrUpdateAsync(queueES).ConfigureAwait(false);

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

        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync().ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitES> GetSignaturCreationUnitESAsync(Guid id) => await _signaturCreationUnitESRepository.GetAsync(id).ConfigureAwait(false);

        public async Task<IEnumerable<ftSignaturCreationUnitES>> GetSignaturCreationUnitESListAsync() => await _signaturCreationUnitESRepository.GetAsync().ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitESAsync(ftSignaturCreationUnitES scu) => await _signaturCreationUnitESRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

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
