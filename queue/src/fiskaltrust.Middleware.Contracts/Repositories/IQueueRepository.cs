using System;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Contracts.Repositories
{
    public interface ICountrySpecificQueueRepository
    {
        Task InsertOrUpdateQueueAsync(ICountrySpecificQueue countrySpecificQueue);

        Task<ICountrySpecificQueue> GetQueueAsync(Guid queueId);
    }
}
