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
            return new ftQueueES
            {
                ftQueueId = queueId,
                CashBoxIdentification = "CashBoxIdentification",
            };
        }

        public async Task InsertOrUpdateQueueAsync(ICountrySpecificQueue queue)
        {
            throw new NotImplementedException();
        }
    }

    public class ftQueueES : ICountrySpecificQueue
    {
        public Guid ftQueueId { get; set; }
        public Guid? ftSignaturCreationUnitId { get; set; }
        public string LastHash { get; set; }
        public string CashBoxIdentification { get; set; }
        public int SSCDFailCount { get; set; }
        public DateTime? SSCDFailMoment { get; set; }
        public Guid? SSCDFailQueueItemId { get; set; }
        public int UsedFailedCount { get; set; }
        public DateTime? UsedFailedMomentMin { get; set; }
        public DateTime? UsedFailedMomentMax { get; set; }
        public Guid? UsedFailedQueueItemId { get; set; }
    }
}
