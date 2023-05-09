using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Repositories
{
    public class QueueRepository : IQueueRepository
    {
        private readonly IConfigurationRepository _configurationRepository;

        public QueueRepository(IConfigurationRepository configurationRepository) 
        {
            _configurationRepository = configurationRepository;
        }

        public async Task<IQueue> GetQueueAsync(Guid queueId) => await _configurationRepository.GetQueueITAsync(queueId).ConfigureAwait(false);
        public async Task InsertOrUpdateQueueAsync(IQueue queue) => await _configurationRepository.InsertOrUpdateQueueITAsync((ftQueueIT) queue).ConfigureAwait(false);
    }
}
