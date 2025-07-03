using System;

namespace fiskaltrust.storage.V0.MasterData
{
    public class PosSystemMasterData
    {
        public Guid PosSystemId { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SoftwareVersion { get; set; }
        public string BaseCurrency { get; set; }
        public string Type { get; set; }
    }
}