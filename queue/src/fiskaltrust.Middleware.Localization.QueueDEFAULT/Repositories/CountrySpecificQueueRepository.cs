using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Repositories
{
    public class CountrySpecificQueueRepository : ICountrySpecificQueueRepository
    {
        private readonly IConfigurationRepository _configurationRepository;

        public CountrySpecificQueueRepository(IConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        public Task<ICountrySpecificQueue> GetQueueAsync(Guid queueId) 
            => Task.FromResult<ICountrySpecificQueue>(new CountryDefaultQueue { CashBoxIdentification = queueId.ToString() });

        public Task InsertOrUpdateQueueAsync(ICountrySpecificQueue countrySpecificQueue) => Task.CompletedTask;

    }
}
