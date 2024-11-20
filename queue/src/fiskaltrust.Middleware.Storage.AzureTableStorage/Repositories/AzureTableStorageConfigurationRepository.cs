using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories
{
    public class AzureTableStorageConfigurationRepository : IConfigurationRepository
    {
        private readonly AzureTableStorageCashBoxRepository _cashBoxRepository;
        private readonly AzureTableStorageQueueRepository _queueRepository;
        private readonly AzureTableStorageQueueATRepository _queueATRepository;
        private readonly AzureTableStorageQueueDERepository _queueDERepository;
        private readonly AzureTableStorageQueueFRRepository _queueFRRepository;
        private readonly AzureTableStorageQueueITRepository _queueITRepository;
        private readonly AzureTableStorageQueueMERepository _queueMERepository;
        private readonly AzureTableStorageSignaturCreationUnitATRepository _signaturCreationUnitATRepository;
        private readonly AzureTableStorageSignaturCreationUnitDERepository _signaturCreationUnitDERepository;
        private readonly AzureTableStorageSignaturCreationUnitFRRepository _signaturCreationUnitFRRepository;
        private readonly AzureTableStorageSignaturCreationUnitITRepository _signaturCreationUnitITRepository;
        private readonly AzureTableStorageSignaturCreationUnitMERepository _signaturCreationUnitMERepository;

        public AzureTableStorageConfigurationRepository() { }

        public AzureTableStorageConfigurationRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
        {
            _cashBoxRepository = new AzureTableStorageCashBoxRepository(queueConfig, tableServiceClient);
            _queueRepository = new AzureTableStorageQueueRepository(queueConfig, tableServiceClient);
            _queueATRepository = new AzureTableStorageQueueATRepository(queueConfig, tableServiceClient);
            _queueDERepository = new AzureTableStorageQueueDERepository(queueConfig, tableServiceClient);
            _queueFRRepository = new AzureTableStorageQueueFRRepository(queueConfig, tableServiceClient);
            _queueITRepository = new AzureTableStorageQueueITRepository(queueConfig, tableServiceClient);
            _queueMERepository = new AzureTableStorageQueueMERepository(queueConfig, tableServiceClient);
            _signaturCreationUnitATRepository = new AzureTableStorageSignaturCreationUnitATRepository(queueConfig, tableServiceClient);
            _signaturCreationUnitDERepository = new AzureTableStorageSignaturCreationUnitDERepository(queueConfig, tableServiceClient);
            _signaturCreationUnitFRRepository = new AzureTableStorageSignaturCreationUnitFRRepository(queueConfig, tableServiceClient);
            _signaturCreationUnitITRepository = new AzureTableStorageSignaturCreationUnitITRepository(queueConfig, tableServiceClient);
            _signaturCreationUnitMERepository = new AzureTableStorageSignaturCreationUnitMERepository(queueConfig, tableServiceClient);
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

        public async Task<ftQueueME> GetQueueMEAsync(Guid queueMEId) => await _queueMERepository.GetAsync(queueMEId).ConfigureAwait(false);
        public async Task<IEnumerable<ftQueueME>> GetQueueMEListAsync() => await _queueMERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateQueueMEAsync(ftQueueME queue) => await _queueMERepository.InsertOrUpdateAsync(queue).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitAT> GetSignaturCreationUnitATAsync(Guid id) => await _signaturCreationUnitATRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitAT>> GetSignaturCreationUnitATListAsync() => await _signaturCreationUnitATRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitATAsync(ftSignaturCreationUnitAT scu) => await _signaturCreationUnitATRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitDE> GetSignaturCreationUnitDEAsync(Guid id) => await _signaturCreationUnitDERepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitDE>> GetSignaturCreationUnitDEListAsync() => await _signaturCreationUnitDERepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitDEAsync(ftSignaturCreationUnitDE scu) => await _signaturCreationUnitDERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitFR> GetSignaturCreationUnitFRAsync(Guid id) => await _signaturCreationUnitFRRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitFR>> GetSignaturCreationUnitFRListAsync() => await _signaturCreationUnitFRRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitFRAsync(ftSignaturCreationUnitFR scu) => await _signaturCreationUnitFRRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task<ftSignaturCreationUnitIT> GetSignaturCreationUnitITAsync(Guid id) => await _signaturCreationUnitITRepository.GetAsync(id).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitIT>> GetSignaturCreationUnitITListAsync() => await _signaturCreationUnitITRepository.GetAsync().ConfigureAwait(false);
        public async Task InsertOrUpdateSignaturCreationUnitITAsync(ftSignaturCreationUnitIT scu) => await _signaturCreationUnitITRepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);

        public async Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => await _signaturCreationUnitMERepository.InsertOrUpdateAsync(scu).ConfigureAwait(false);
        public async Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => await _signaturCreationUnitMERepository.GetAsync().ConfigureAwait(false);
        public async Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitMEId) => await _signaturCreationUnitMERepository.GetAsync(signaturCreationUnitMEId).ConfigureAwait(false);
    }
}

