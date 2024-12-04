using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RegistroFacturacionAnulacion
{

    [XmlElement(Order = 0)]
    public required Version IDVersion { get; set; }

    [XmlElement(Order = 1)]
    public required IDFacturaExpedidaBaja IDFactura { get; set; }

    [XmlElement(Order = 2)]
    public string? RefExterna { get; set; }

    [XmlElement(Order = 3)]
    public Booleano? SinRegistroPrevio { get; set; }

    [XmlElement(Order = 4)]
    public Booleano? RechazoPrevio { get; set; }

    [XmlElement(Order = 5)]
    public GeneradoPor? GeneradoPor { get; set; }

    [XmlElement(Order = 6)]
    public PersonaFisicaJuridica? Generador { get; set; }

    [XmlElement(Order = 7)]
    public required RegistroFacturacionAnulacionEncadenamiento Encadenamiento { get; set; }

    [XmlElement(Order = 8)]
    public required SistemaInformatico SistemaInformatico { get; set; }

    [XmlIgnore]
    public required DateTime FechaHoraHusoGenRegistro { get; set; }

    [XmlElement("FechaHoraHusoGenRegistro", Order = 25)]
    public string FechaHoraHusoGenRegistroString
    {
        get => FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddTHH:mm:sszzz");
        set => FechaHoraHusoGenRegistro = DateTime.Parse(value);
    }

    [XmlElement(Order = 10)]
    public required TipoHuella TipoHuella { get; set; }

    [XmlElement(Order = 11)]
    public required string Huella { get; set; }

    [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 12)]
    public XmlElement? Signature { get; set; }
}
