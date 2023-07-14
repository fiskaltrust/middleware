using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueIT.Constants
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
