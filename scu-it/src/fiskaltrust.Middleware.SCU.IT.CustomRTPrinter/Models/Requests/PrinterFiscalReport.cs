using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerFiscalReport")]
    public class PrinterFiscalReport : IRequest
    {
        public PrinterFiscalReport() { }

        public PrinterFiscalReport(IReport report)
        {
            Report = new Records<IReport>(new[] { report });
        }

        [XmlElement("dematerializedOn", IsNullable = false)]
        public DematerializedOn DematerializedOn { get; set; } = new();

        [XmlAnyElement()]
        public Records<IReport> Report { get; set; }
    }

    public interface IReport : IRecord { }
}