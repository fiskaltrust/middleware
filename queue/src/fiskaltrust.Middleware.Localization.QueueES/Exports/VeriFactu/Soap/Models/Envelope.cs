using System.Xml.Serialization;
using fiskaltrust.Middleware.SCU.ES.Helpers;

namespace fiskaltrust.Middleware.SCU.ES.Soap;

[Serializable]
[XmlRoot("Envelope", Namespace = NAMESPACE)]
public class Envelope
{
    public const string NAMESPACE = "http://schemas.xmlsoap.org/soap/envelope/";

    public Envelope()
    {
    }

    [XmlElement(Order = 1)]
    public Header Header { get; set; } = new Header();

    [XmlElement(Order = 2)]
    public required Body Body { get; set; }

    public string Serialize()
    {
        var serializer = new XmlSerializer(GetType());
        using var writer = new Utf8StringWriter();

        serializer.Serialize(writer, this);

        return writer.ToString();
    }
}