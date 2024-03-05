using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Contracts.Constants
{
    public interface ICountrySpecificSettings
    {
        public long CountryBaseState { get; }

        public bool ResendFailedReceipts { get; }

        public ICountrySpecificQueueRepository CountrySpecificQueueRepository { get; }
    }
}
