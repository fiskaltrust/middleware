using System;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtSignaturCreationUnitFR : TableEntity
    {
        public Guid ftSignaturCreationUnitFRId { get; set; }
        public string Siret { get; set; }
        public string PrivateKey { get; set; }
        public string CertificateBase64 { get; set; }
        public string CertificateSerialNumber { get; set; }
        public long TimeStamp { get; set; }
    }
}
