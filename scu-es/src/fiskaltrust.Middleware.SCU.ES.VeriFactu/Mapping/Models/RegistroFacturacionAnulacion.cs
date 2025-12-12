using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RegistroFacturacionAnulacion
{

    [XmlElement(Order = 0)]
    public required Version IDVersion { get; set; }

    [XmlElement(Order = 1)]
    public required IDFacturaExpedidaBaja IDFactura { get; set; }

    [XmlElement(Order = 2)]
    public string? RefExterna { get; set; }
    [XmlIgnore]
    public bool RefExternaSpecified => RefExterna is not null;

    [XmlElement(Order = 3)]
    public Booleano? SinRegistroPrevio { get; set; }
    [XmlIgnore]
    public bool SinRegistroPrevioSpecified => SinRegistroPrevio is not null;

    [XmlElement(Order = 4)]
    public Booleano? RechazoPrevio { get; set; }
    [XmlIgnore]
    public bool RechazoPrevioSpecified => RechazoPrevio is not null;

    [XmlElement(Order = 5)]
    public GeneradoPor? GeneradoPor { get; set; }
    [XmlIgnore]
    public bool GeneradoPorSpecified => GeneradoPor is not null;

    [XmlElement(Order = 6)]
    public PersonaFisicaJuridica? Generador { get; set; }
    [XmlIgnore]
    public bool GeneradorSpecified => Generador is not null;

    [XmlElement(Order = 7)]
    public required RegistroFacturacionAnulacionEncadenamiento Encadenamiento { get; set; }

    [XmlElement(Order = 8)]
    public required SistemaInformatico SistemaInformatico { get; set; }

    [XmlIgnore]
    public required DateTimeOffset FechaHoraHusoGenRegistro { get; set; }

    [XmlElement("FechaHoraHusoGenRegistro", Order = 9)]
    public string FechaHoraHusoGenRegistroString
    {
        get => FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddTHH:mm:sszzz");
        set => FechaHoraHusoGenRegistro = DateTimeOffset.Parse(value);
    }

    [XmlElement(Order = 10)]
    public required TipoHuella TipoHuella { get; set; }

    [XmlElement(Order = 11)]
    public required string Huella { get; set; }

    [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 12)]
    public XmlElement? Signature { get; set; }
    [XmlIgnore]
    public bool SignatureSpecified => Signature is not null;
}
