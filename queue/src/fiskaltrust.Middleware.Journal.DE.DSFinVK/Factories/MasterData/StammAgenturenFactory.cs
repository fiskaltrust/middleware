using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammAgenturenFactory
    {
        private readonly DSFinVKParameters _parameters;

        public StammAgenturenFactory(DSFinVKParameters parameters)
        {
            _parameters = parameters;
        }

        public IEnumerable<StammAgentur> Create(MasterDataConfiguration masterData, long zNumber, DateTime dailyClosingMoment)
        {
            foreach (var agency in masterData?.Agencies ?? Enumerable.Empty<AgencyMasterData>())
            {
                yield return new StammAgentur
                {
                    Z_KASSE_ID = _parameters.CashboxIdentification,
                    Z_ERSTELLUNG = dailyClosingMoment,
                    Z_NR = zNumber,
                    AGENTUR_ID = AgencyIdProvider.GetIdByGuid(agency.AgencyId.ToString()),
                    AGENTUR_NAME = agency.Name.SafeSubstring(0, 60),
                    AGENTUR_STRASSE = agency.Street,
                    AGENTUR_PLZ = agency.Zip,
                    AGENTUR_ORT = agency.City,
                    AGENTUR_LAND = Conversion.From2DigitsTo3DigitsCountryCode(agency.Country),
                    AGENTUR_STNR = agency.TaxId,
                    AGENTUR_USTID = agency.VatId
                };
            }
        }
    }
}
