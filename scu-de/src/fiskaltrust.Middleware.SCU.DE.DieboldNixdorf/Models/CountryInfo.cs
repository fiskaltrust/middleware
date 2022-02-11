using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class CountryInfo
    {
        public long ApiMajorVersion { get; set; }
        public long ApiMinorVersion { get; set; }
        public long CountryId { get; set; }
        public DieboldNixdorfHardwareId HardwareId { get; set; }
    }
}
