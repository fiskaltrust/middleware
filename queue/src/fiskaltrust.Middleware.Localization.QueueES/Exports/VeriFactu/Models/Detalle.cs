using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class Detalle
{
    [XmlElement(Order = 0)]
    public Impuesto? Impuesto { get; set; }

    [XmlElement(Order = 1)]
    public IdOperacionesTrascendenciaTributaria? ClaveRegimen { get; set; }

    [XmlElement("CalificacionOperacion", typeof(CalificacionOperacion), Order = 2)]
    [XmlElement("OperacionExenta", typeof(OperacionExenta), Order = 2)]
    public required object Item { get; set; }

    [XmlElement(Order = 3)]
    public string? TipoImpositivo { get; set; }

    [XmlElement(Order = 4)]
    public required string BaseImponibleOimporteNoSujeto { get; set; }

    [XmlElement(Order = 5)]
    public string? BaseImponibleACoste { get; set; }

    [XmlElement(Order = 6)]
    public string? CuotaRepercutida { get; set; }

    [XmlElement(Order = 7)]
    public string? TipoRecargoEquivalencia { get; set; }

    [XmlElement(Order = 8)]
    public string? CuotaRecargoEquivalencia { get; set; }
}