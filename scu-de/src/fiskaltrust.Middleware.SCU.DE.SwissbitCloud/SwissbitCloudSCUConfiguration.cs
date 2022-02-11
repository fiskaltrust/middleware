using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud
{
    public class SwissbitCloudSCUConfiguration : DeutscheFiskalSCUConfiguration
    {
        public override string CertificationId { get; set; } = "BSI-K-TR-0456-2021";
    }
}
