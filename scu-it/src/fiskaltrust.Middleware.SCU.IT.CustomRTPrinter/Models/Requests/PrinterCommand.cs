using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    [XmlRoot("printerCommand")]
    public class PrinterCommand : IRequest
    {
        public PrinterCommand() { }

        public PrinterCommand(ICommand command)
        {
            Command = new Records<ICommand>(new[] { command });
        }

        [XmlAnyElement()]
        public Records<ICommand> Command { get; set; }
    }

    public interface ICommand : IRecord { }
}