namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid
{
    public class SwissbitCloudAndroidSCUConfiguration
    {
        public string FccId { get; set; }
        public string FccSecret { get; set; }
        public virtual string CertificationId { get; set; } = "BSI-K-TR-0448-2021";
        public string SscdId { get; set; }
    }
}