using System;

namespace fiskaltrust.storage.V0
{
    public class ftSignaturCreationUnitFR
    {
        public Guid ftSignaturCreationUnitFRId { get; set; }

        public string Siret { get; set; }

        public string PrivateKey { get; set; }

        public string CertificateBase64 { get; set; }

        public string CertificateSerialNumber { get; set; }

        public long TimeStamp { get; set; }
    }
}
