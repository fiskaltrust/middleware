using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Dummy;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Repositories
{
    /// <summary>
    /// Class responsible for managing country-specific queues.
    /// </summary>
    public class CountrySpecificQueueRepository : ICountrySpecificQueueRepository
    {
        private readonly IConfigurationRepository _configurationRepository;

        public CountrySpecificQueueRepository(IConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }
        
        /// <summary>
        /// Retrieves a specific queue by its unique identifier (queueId).
        /// </summary>
        /// <remarks>
        /// Returns a dummy queue with the provided identification.
        /// In a real market, this method should retrieve the ftQueueXX from the storage.
        /// For an example of the implementation, refer to <see cref="fiskaltrust.Middleware.Localization.QueueIT.Repositories.CountrySpecificQueueRepository"/> in the Italian market folder.
        /// </remarks>
        public Task<ICountrySpecificQueue> GetQueueAsync(Guid queueId) 
            => Task.FromResult<ICountrySpecificQueue>(new CountryDefaultQueue { CashBoxIdentification = queueId.ToString()});
        
        /// <summary>
        /// Inserts or updates a country-specific queue.
        /// </summary>
        public Task InsertOrUpdateQueueAsync(ICountrySpecificQueue countrySpecificQueue) => Task.CompletedTask;
    }
}