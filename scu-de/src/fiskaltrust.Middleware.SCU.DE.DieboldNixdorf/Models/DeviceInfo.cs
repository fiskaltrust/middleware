namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class DeviceInfo
    {
        public string SerialNo { get; set; }
        public long NumSlots { get; set; }
        public long NumSlotsOccupied { get; set; }
        public long NumSlotsMaint { get; set; }
        public long NumSlotsTseInserted { get; set; }
        public string PcbVersion { get; set; }
        public string IpAddress { get; set; }
    }
}