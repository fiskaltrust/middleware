using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration
{
    public class AzureFtSignaturCreationUnitME : TableEntity
    {
        public Guid ftSignaturCreationUnitMEId { get; set; }
        public string Url { get; set; }
        public long TimeStamp { get; set; }
        public string IssuerTin { get; set; }
        public string BusinessUnitCode { get; set; }
        public string TcrIntId { get; set; }
        public string SoftwareCode { get; set; }
        public string MaintainerCode { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string TcrCode { get; set; }
    }
}
