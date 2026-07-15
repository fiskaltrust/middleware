using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceiptRefundSpecial : IRequest
    {
        public PrinterFiscalReceiptRefundSpecial() { }

        public PrinterFiscalReceiptRefundSpecial(BeginRtDocRefundSpecial begin, IFiscalRecord[] records)
        {
            BeginRtDocRefundSpecial = begin;
            Records = new Records<IFiscalRecord>(records);
        }

        [XmlElement("dematerializedOn", IsNullable = false)]
        public DematerializedOn DematerializedOn { get; set; } = new();

        [XmlElement("beginRtDocRefundSpecial")]
        public BeginRtDocRefundSpecial BeginRtDocRefundSpecial { get; set; } = new();

        [XmlAnyElement()]
        public Records<IFiscalRecord> Records { get; set; }

        [XmlElement("endFiscalReceiptCut")]
        public EndFiscalReceiptCut EndFiscalReceiptCut { get; set; } = new();
    }

    [XmlRoot("beginRtDocRefundSpecial")]
    public class BeginRtDocRefundSpecial
    {
        [XmlAttribute("service")]
        public string Service { get; set; } = "ND";

        // Date in DDMMYY format
        [XmlAttribute("docDate")]
        public string DocDate { get; set; }
    }

    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceiptRefund : IRequest
    {
        public PrinterFiscalReceiptRefund() { }

        public PrinterFiscalReceiptRefund(BeginRtDocRefund begin, IFiscalRecord[] records)
        {
            BeginRtDocRefund = begin;
            Records = new Records<IFiscalRecord>(records);
        }

        [XmlElement("dematerializedOn", IsNullable = false)]
        public DematerializedOn DematerializedOn { get; set; } = new();

        [XmlElement("beginRtDocRefund")]
        public BeginRtDocRefund BeginRtDocRefund { get; set; } = new();

        [XmlAnyElement()]
        public Records<IFiscalRecord> Records { get; set; }

        [XmlElement("endFiscalReceiptCut")]
        public EndFiscalReceiptCut EndFiscalReceiptCut { get; set; } = new();
    }

    [XmlRoot("beginRtDocRefund")]
    public class BeginRtDocRefund
    {
        [XmlAttribute("docRefZ")]
        public string DocRefZ { get; set; }

        [XmlAttribute("docRefNumber")]
        public string DocRefNumber { get; set; }

        // Date of the referenced document in DDMMYY format
        [XmlAttribute("docDate")]
        public string DocDate { get; set; }

        [XmlAttribute("printPreview")]
        public int PrintPreview { get; set; } = 0;

        [XmlAttribute("fiscalSerial")]
        public string FiscalSerial { get; set; }

        [XmlAttribute("checkOnly")]
        public int CheckOnly { get; set; } = 0;
    }
}
