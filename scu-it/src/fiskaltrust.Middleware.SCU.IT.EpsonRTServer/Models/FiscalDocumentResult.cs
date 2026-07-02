using System;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models
{
    /// <summary>Result of mapping a ReceiptRequest to an Epson RT Server createReceipt command.</summary>
    public class FiscalDocumentResult
    {
        /// <summary>The full &lt;createReceipt&gt; body ready to be sent to fpserver.cgi.</summary>
        public string CreateReceiptXml { get; set; } = string.Empty;

        /// <summary>The locally computed CCDC (SHA-256 fingerprint) for this document. Becomes the next Section A.</summary>
        public string Ccdc { get; set; } = string.Empty;

        /// <summary>Section A used for this document (token or previous CCDC).</summary>
        public string PreviousFingerPrint { get; set; } = string.Empty;

        public long DocNumber { get; set; }

        public long ZNumber { get; set; }

        /// <summary>0 = sale, 1 = refund, 3 = void (fiscalInformation docType).</summary>
        public int DocType { get; set; }

        public DateTime DocMoment { get; set; }

        public long AmountCents { get; set; }

        public string LotteryCode { get; set; } = string.Empty;

        public long? ReferenceZNumber { get; set; }

        public long? ReferenceDocNumber { get; set; }

        public DateTime? ReferenceDocMoment { get; set; }
    }
}
