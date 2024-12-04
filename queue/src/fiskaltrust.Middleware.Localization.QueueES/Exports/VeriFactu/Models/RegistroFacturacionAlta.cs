using System.Xml;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RegistroFacturacionAlta
{
    [XmlElement(Order = 0)]
    public required Version IDVersion { get; set; }

    [XmlElement(Order = 1)]
    public required IDFactura IDFactura { get; set; }

    [XmlElement(Order = 2)]
    public string? RefExterna { get; set; }

    [XmlElement(Order = 3)]
    public required string NombreRazonEmisor { get; set; }

    [XmlElement(Order = 4)]
    public Booleano? Subsanacion { get; set; }

    [XmlElement(Order = 5)]
    public RechazoPrevio? RechazoPrevio { get; set; }

    [XmlElement(Order = 6)]
    public required ClaveTipoFactura TipoFactura { get; set; }

    [XmlElement(Order = 7)]
    public ClaveTipoRectificativa? TipoRectificativa { get; set; }

    [XmlArray(Order = 8)]
    [XmlArrayItem("IDFacturaRectificada", IsNullable = false)]
    public IDFactura[]? FacturasRectificadas { get; set; }

    [XmlArray(Order = 9)]
    [XmlArrayItem("IDFacturaSustituida", IsNullable = false)]
    public IDFactura[]? FacturasSustituidas { get; set; }

    [XmlElement(Order = 10)]
    public DesgloseRectificacion? ImporteRectificacion { get; set; }

    [XmlElement(Order = 11)]
    public string? FechaOperacion { get; set; }

    [XmlElement(Order = 12)]
    public required string DescripcionOperacion { get; set; }

    [XmlElement(Order = 13)]
    public Booleano? FacturaSimplificadaArt7273 { get; set; }

    [XmlElement(Order = 14)]
    public Booleano? FacturaSinIdentifDestinatarioArt61d { get; set; }

    [XmlElement(Order = 15)]
    public Booleano? Macrodato { get; set; }

    [XmlElement(Order = 16)]
    public TercerosODestinatario? EmitidaPorTerceroODestinatario { get; set; }

    [XmlElement(Order = 17)]
    public PersonaFisicaJuridica? Tercero { get; set; }

    [XmlArray(Order = 18)]
    [XmlArrayItem("IDDestinatario", IsNullable = false)]
    public PersonaFisicaJuridica[]? Destinatarios { get; set; }

    [XmlElement(Order = 19)]
    public Booleano? Cupon { get; set; }

    [XmlArray(Order = 20)]
    [XmlArrayItem("DetalleDesglose", IsNullable = false)]
    public required Detalle[] Desglose { get; set; }

    [XmlElement(Order = 21)]
    public required string CuotaTotal { get; set; }

    [XmlElement(Order = 22)]
    public required string ImporteTotal { get; set; }

    [XmlElement(Order = 23)]
    public required RegistroFacturacionAltaEncadenamiento Encadenamiento { get; set; }

    [XmlElement(Order = 24)]
    public required SistemaInformatico SistemaInformatico { get; set; }

    [XmlIgnore]
    public required DateTime FechaHoraHusoGenRegistro { get; set; }

    [XmlElement("FechaHoraHusoGenRegistro", Order = 25)]
    public string FechaHoraHusoGenRegistroString
    {
        get => FechaHoraHusoGenRegistro.ToString("yyyy-MM-ddTHH:mm:sszzz");
        set => FechaHoraHusoGenRegistro = DateTime.Parse(value);
    }

    [XmlElement(Order = 26)]
    public string? NumRegistroAcuerdoFacturacion { get; set; }

    [XmlElement(Order = 27)]
    public string? IdAcuerdoSistemaInformatico { get; set; }

    [XmlElement(Order = 28)]
    public required TipoHuella TipoHuella { get; set; }

    [XmlElement(Order = 29)]
    public required string Huella { get; set; }

    [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#", Order = 30)]
    public XmlElement? Signature { get; set; }
}
