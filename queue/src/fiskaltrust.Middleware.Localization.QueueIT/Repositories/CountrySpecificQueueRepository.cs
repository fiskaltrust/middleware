using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.Repositories
{
    public class CountrySpecificQueueRepository : ICountrySpecificQueueRepository
    {
        private readonly IConfigurationRepository _configurationRepository;

        public CountrySpecificQueueRepository(IConfigurationRepository configurationRepository) 
        {
            _configurationRepository = configurationRepository;
        }

        public async Task<ICountrySpecificQueue> GetQueueAsync(Guid queueId) => await _configurationRepository.GetQueueITAsync(queueId).ConfigureAwait(false);
        public async Task InsertOrUpdateQueueAsync(ICountrySpecificQueue queue) => await _configurationRepository.InsertOrUpdateQueueITAsync((ftQueueIT) queue).ConfigureAwait(false);
    }
}
