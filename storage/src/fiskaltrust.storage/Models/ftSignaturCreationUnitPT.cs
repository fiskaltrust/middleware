using System;

namespace fiskaltrust.storage.V0
{
    public class ftSignaturCreationUnitPTConfiguration
    {
        public Guid ftSignaturCreationUnitPTId { get; set; }
    }

    public class ftSignaturCreationUnitPT
    {
        public Guid ftSignaturCreationUnitPTId { get; set; }

        public string PrivateKey { get; set; }

        public string SoftwareCertificateNumber { get; set; }

        public long TimeStamp { get; set; }
    }
}
