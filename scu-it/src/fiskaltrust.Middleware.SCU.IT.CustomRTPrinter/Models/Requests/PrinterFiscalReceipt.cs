using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceipt : IRequest
    {
        public PrinterFiscalReceipt() { }

        public PrinterFiscalReceipt(IFiscalRecord[] records)
        {
            Records = new Records<IFiscalRecord>(records);
        }

        [XmlElement("beginFiscalReceipt")]
        public BeginFiscalReceipt BeginFiscalReceipt { get; set; } = new();

        [XmlAnyElement()]
        public Records<IFiscalRecord> Records { get; set; }

        [XmlElement("endFiscalReceipt")]
        public EndFiscalReceipt EndFiscalReceipt { get; set; } = new();
    }

    public interface IFiscalRecord : IRecord { }

    [XmlRoot("beginFiscalReceipt")]
    public class BeginFiscalReceipt
    {
    }

    [XmlRoot("endFiscalReceipt")]
    public class EndFiscalReceipt
    {
    }
}