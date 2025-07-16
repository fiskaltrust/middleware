using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuSoap;

[Serializable]
[XmlRoot("Envelope", Namespace = NAMESPACE)]
public class Envelope<T>
{
    public const string NAMESPACE = "http://schemas.xmlsoap.org/soap/envelope/";

    [XmlElement(Order = 1)]
    public Header Header { get; set; } = new Header();

    [XmlElement(Order = 2)]
    public required T Body { get; set; }
}