using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.Repositories
{
    public class CountrySpecificQueueRepository : ICountrySpecificQueueRepository
    {
        private readonly IConfigurationRepository _configurationRepository;

        public CountrySpecificQueueRepository(IConfigurationRepository configurationRepository) 
        {
            _configurationRepository = configurationRepository;
        }

        public async Task<ICountrySpecificQueue> GetQueueAsync(Guid queueId)
        {
            throw new NotImplementedException();
        }

        public async Task InsertOrUpdateQueueAsync(ICountrySpecificQueue queue)
        {
            throw new NotImplementedException();
        }
    }
}
