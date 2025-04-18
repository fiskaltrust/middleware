using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(AnonymousType = true, Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd")]
public class RegFactuSistemaFacturacion
{
    [XmlElement(Order = 0)]
    public required Cabecera Cabecera { get; set; }

    [XmlElement("RegistroFactura", Order = 1)]
    public required RegistroFactura[] RegistroFactura { get; set; }
}
