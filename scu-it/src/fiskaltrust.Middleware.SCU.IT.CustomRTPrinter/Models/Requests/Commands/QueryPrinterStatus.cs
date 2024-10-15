using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("queryPrinterStatus")]
    public class QueryPrinterStatus : ICommand
    {

    }
}