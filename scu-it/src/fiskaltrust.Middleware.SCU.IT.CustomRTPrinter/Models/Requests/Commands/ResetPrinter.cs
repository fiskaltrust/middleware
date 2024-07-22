using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("resetPrinter")]
    public class ResetPrinter : ICommand
    {

    }
}