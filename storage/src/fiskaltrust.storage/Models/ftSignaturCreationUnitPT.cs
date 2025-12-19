using System;

namespace fiskaltrust.storage.V0
{
    public class ftSignaturCreationUnitPT
    {
        public Guid ftSignaturCreationUnitPTId { get; set; }

        public string PrivateKey { get; set; }

        public string SoftwareCertificateNumber { get; set; }

        public string Url { get; set; }

        public long TimeStamp { get; set; }
    }
}
