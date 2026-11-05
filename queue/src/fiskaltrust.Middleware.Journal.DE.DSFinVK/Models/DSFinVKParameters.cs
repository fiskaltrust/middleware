
namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models
{
    public class DSFinVKParameters
    {
        public string CashboxIdentification { get; set; }

        public string TSECertificateBase64 { get; set; }

        public int FirstZNumber { get; set; }
        //In the portal this is the receiptnumber, in the middleware it is the receiptmoment
        public long From { get; set; }
        //In the portal this is the receiptnumber, in the middleware it is the receiptmoment
        public long To { get; set; }

        public string MiddlewareVersion { get; set; } = "1.3";

        public string TargetDirectory { get; set; } = "./";

        public Common.Helpers.ReferencesLookUpType ReferencesLookUpType { get; set; } = Common.Helpers.ReferencesLookUpType.AddReferences;
    
        public bool IncludeOrders { get; set; } = true;

        public string PublicKeyBase64 { get; set; }

        public string SignAlgorithm { get; set; }

        public string TimeFormat { get; set; }

        /// <summary>
        /// When set to true, the exports will skip duplicated receipts, using the same algorithm as the Middleware does when using the receipt request flag
        /// </summary>
        public bool SkipDuplicatedReceipts { get; set; }
    }
}
