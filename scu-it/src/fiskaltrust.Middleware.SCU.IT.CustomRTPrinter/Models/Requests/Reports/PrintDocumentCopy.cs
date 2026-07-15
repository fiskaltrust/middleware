using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests
{
    // Prints a copy of the last fiscal receipt (no parameters — printer always reprints the last document).
    [XmlRoot("printDuplicateReceipt")]
    public class PrintDuplicateReceipt : IReport
    {
    }
}
