using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReport")]
    public class PrinterFiscalReport : IRequest
    {
        public PrinterFiscalReport() { }

        public PrinterFiscalReport(IReport[] reports)
        {
            Reports = new Records<IReport>(reports);
        }

        [XmlAnyElement()]
        public Records<IReport> Reports { get; set; }
    }

    public interface IReport : IRecord { }
}