using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class Detalle
{
    [XmlElement(Order = 0)]
    public Impuesto? Impuesto { get; set; }
    [XmlIgnore]
    public bool ImpuestoSpecified => Impuesto is not null;

    [XmlElement(Order = 1)]
    public IdOperacionesTrascendenciaTributaria? ClaveRegimen { get; set; }
    [XmlIgnore]
    public bool ClaveRegimenSpecified => ClaveRegimen is not null;

    [XmlElement("CalificacionOperacion", typeof(CalificacionOperacion), Order = 2)]
    [XmlElement("OperacionExenta", typeof(OperacionExenta), Order = 2)]
    public required object Item { get; set; }

    [XmlElement(Order = 3)]
    public string? TipoImpositivo { get; set; }
    [XmlIgnore]
    public bool TipoImpositivoSpecified => TipoImpositivo is not null;

    [XmlElement(Order = 4)]
    public required string BaseImponibleOimporteNoSujeto { get; set; }

    [XmlElement(Order = 5)]
    public string? BaseImponibleACoste { get; set; }
    [XmlIgnore]
    public bool BaseImponibleACosteSpecified => BaseImponibleACoste is not null;

    [XmlElement(Order = 6)]
    public string? CuotaRepercutida { get; set; }
    [XmlIgnore]
    public bool CuotaRepercutidaSpecified => CuotaRepercutida is not null;

    [XmlElement(Order = 7)]
    public string? TipoRecargoEquivalencia { get; set; }
    [XmlIgnore]
    public bool TipoRecargoEquivalenciaSpecified => TipoRecargoEquivalencia is not null;

    [XmlElement(Order = 8)]
    public string? CuotaRecargoEquivalencia { get; set; }
    [XmlIgnore]
    public bool CuotaRecargoEquivalenciaSpecified => CuotaRecargoEquivalencia is not null;
}