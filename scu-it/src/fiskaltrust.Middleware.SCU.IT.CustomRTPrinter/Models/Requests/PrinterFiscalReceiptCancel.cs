using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReceipt")]
    public class PrinterFiscalReceiptCancel : IRequest
    {
        [XmlElement("cancelFiscalReceipt")]
        public CancelFiscalReceipt Cancel { get; set; } = new();
    }

    [XmlRoot("cancelFiscalReceipt")]
    public class CancelFiscalReceipt
    {
    }
}
