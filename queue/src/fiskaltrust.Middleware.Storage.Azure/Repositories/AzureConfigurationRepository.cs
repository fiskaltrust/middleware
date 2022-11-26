using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration;
using fiskaltrust.storage.V0;
using Azure.Data.Tables;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public class AzureConfigurationRepository : IConfigurationRepository
    {
        private readonly BaseAzureTableRepository<Guid, AzureFtCashBox, ftCashBox> _cashBoxRepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtQueue, ftQueue> _queueRepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtQueueAT, ftQueueAT> _queueATRepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtQueueDE, ftQueueDE> _queueDERepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtQueueFR, ftQueueFR> _queueFRRepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitAT, ftSignaturCreationUnitAT> _signaturCreationUnitATRepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitDE, ftSignaturCreationUnitDE> _signaturCreationUnitDERepository;
        private readonly BaseAzureTableRepository<Guid, AzureFtSignaturCreationUnitFR, ftSignaturCreationUnitFR> _signaturCreationUnitFRRepository;

        public AzureConfigurationRepository() { }

        public AzureConfigurationRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
        {
            _cashBoxRepository = new AzureCashBoxRepository(queueConfig, tableServiceClient);
            _queueRepository = new AzureQueueRepository(queueConfig, tableServiceClient);
            _queueATRepository = new AzureQueueATRepository(queueConfig, tableServiceClient);
            _queueDERepository = new AzureQueueDERepository(queueConfig, tableServiceClient);
            _queueFRRepository = new AzureQueueFRRepository(queueConfig, tableServiceClient);
            _signaturCreationUnitATRepository = new AzureSignaturCreationUnitATRepository(queueConfig, tableServiceClient);
            _signaturCreationUnitDERepository = new AzureSignaturCreationUnitDERepository(queueConfig, tableServiceClient);
            _signaturCreationUnitFRRepository = new AzureSignaturCreationUnitFRRepository(queueConfig, tableServiceClient);
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
        public Task InsertOrUpdateSignaturCreationUnitMEAsync(ftSignaturCreationUnitME scu) => throw new NotImplementedException();
        public Task InsertOrUpdateQueueMEAsync(ftQueueME queue) => throw new NotImplementedException();
        public Task<IEnumerable<ftSignaturCreationUnitME>> GetSignaturCreationUnitMEListAsync() => throw new NotImplementedException();
        public Task<ftSignaturCreationUnitME> GetSignaturCreationUnitMEAsync(Guid signaturCreationUnitDEId) => throw new NotImplementedException();
        public Task<IEnumerable<ftQueueME>> GetQueueMEListAsync() => throw new NotImplementedException();
        public Task<ftQueueME> GetQueueMEAsync(Guid queueMEId) => throw new NotImplementedException();
    }
}
