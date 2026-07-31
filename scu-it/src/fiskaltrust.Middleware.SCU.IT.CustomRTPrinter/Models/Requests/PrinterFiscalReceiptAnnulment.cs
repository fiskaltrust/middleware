using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceiptAnnulmentSpecial : IRequest
    {
        public PrinterFiscalReceiptAnnulmentSpecial() { }

        public PrinterFiscalReceiptAnnulmentSpecial(BeginRtDocAnnulmentSpecial begin, IFiscalRecord[] records)
        {
            BeginRtDocAnnulmentSpecial = begin;
            Records = new Records<IFiscalRecord>(records);
        }

        [XmlElement("dematerializedOn", IsNullable = false)]
        public DematerializedOn DematerializedOn { get; set; } = new();

        [XmlElement("beginRtDocAnnulmentSpecial")]
        public BeginRtDocAnnulmentSpecial BeginRtDocAnnulmentSpecial { get; set; } = new();

        [XmlAnyElement()]
        public Records<IFiscalRecord> Records { get; set; }

        [XmlElement("endFiscalReceiptCut")]
        public EndFiscalReceiptCut EndFiscalReceiptCut { get; set; } = new();
    }

    [XmlRoot("beginRtDocAnnulmentSpecial")]
    public class BeginRtDocAnnulmentSpecial
    {
        [XmlAttribute("service")]
        public string Service { get; set; } = "ND";

        // Date in DDMMYY format
        [XmlAttribute("docDate")]
        public string DocDate { get; set; }
    }

    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceiptAnnulment : IRequest
    {
        public PrinterFiscalReceiptAnnulment() { }

        public PrinterFiscalReceiptAnnulment(BeginRtDocAnnulment begin, IFiscalRecord[] records)
        {
            BeginRtDocAnnulment = begin;
            Records = new Records<IFiscalRecord>(records);
        }

        [XmlElement("dematerializedOn", IsNullable = false)]
        public DematerializedOn DematerializedOn { get; set; } = new();

        [XmlElement("beginRtDocAnnulment")]
        public BeginRtDocAnnulment BeginRtDocAnnulment { get; set; } = new();

        [XmlAnyElement()]
        public Records<IFiscalRecord> Records { get; set; }

        [XmlElement("endFiscalReceiptCut")]
        public EndFiscalReceiptCut EndFiscalReceiptCut { get; set; } = new();
    }

    [XmlRoot("beginRtDocAnnulment")]
    public class BeginRtDocAnnulment
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
