using System;
using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData
{
    public class StammOrteFactory
    {
        private readonly DSFinVKParameters _parameters;

        public StammOrteFactory(DSFinVKParameters paramaters)
        {
            _parameters = paramaters;
        }

        public StammOrt Create(MasterDataConfiguration masterData, long zNumber, DateTime dailyClosingMoment) => new StammOrt
        {
            Z_KASSE_ID = _parameters.CashboxIdentification,
            Z_ERSTELLUNG = dailyClosingMoment,
            Z_NR = zNumber,
            LOC_NAME = masterData?.Outlet?.OutletName,
            LOC_STRASSE = masterData?.Outlet?.Street,
            LOC_PLZ = masterData?.Outlet?.Zip,
            LOC_ORT = masterData?.Outlet?.City,
            LOC_LAND = Conversion.From2DigitsTo3DigitsCountryCode(masterData?.Outlet?.Country),
            LOC_USTID = masterData?.Outlet?.VatId
        };
    }
}
