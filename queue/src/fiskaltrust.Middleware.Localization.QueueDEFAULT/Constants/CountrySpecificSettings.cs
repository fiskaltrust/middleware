using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Constants
{
    public class CountrySpecificSettings : ICountrySpecificSettings
    {
        public long CountryBaseState => Cases.BASE_STATE;

        public bool ResendFailedReceipts => true;

        public ICountrySpecificQueueRepository CountrySpecificQueueRepository { get;  private set; }

        public CountrySpecificSettings(ICountrySpecificQueueRepository countrySpecificQueueRepository)
        {
            CountrySpecificQueueRepository = countrySpecificQueueRepository;
        }
    }
}
