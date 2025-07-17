using System.Collections.Generic;

namespace fiskaltrust.storage.V0.MasterData
{
    public class MasterDataConfiguration
    {
        public AccountMasterData Account { get; set; }
        public OutletMasterData Outlet { get; set; }
        public IEnumerable<AgencyMasterData> Agencies { get; set; }
        public IEnumerable<PosSystemMasterData> PosSystems { get; set; }
    }
}
