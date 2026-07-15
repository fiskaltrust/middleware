using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerNotFiscal")]
    public class PrinterNonFiscal : IRequest
    {
        public PrinterNonFiscal() { }

        public PrinterNonFiscal(INonFiscalRecord[] records)
        {
            Records = new Records<INonFiscalRecord>(records);
        }

        [XmlElement("beginNotFiscal")]
        public BeginNotFiscal BeginNotFiscal { get; set; } = new();

        [XmlAnyElement()]
        public Records<INonFiscalRecord> Records { get; set; }

        [XmlElement("endNotFiscal")]
        public EndNotFiscal EndNotFiscal { get; set; } = new();
    }

    public interface INonFiscalRecord : IRecord { }

    [XmlRoot("beginNotFiscal")]
    public class BeginNotFiscal { }

    [XmlRoot("endNotFiscal")]
    public class EndNotFiscal { }
}
