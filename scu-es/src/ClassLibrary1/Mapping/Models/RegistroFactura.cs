using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd")]
public class RegistroFactura
{
    /// <remarks/>
    [XmlElement("RegistroAlta", typeof(RegistroFacturacionAlta), Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd", Order = 0)]
    [XmlElement("RegistroAnulacion", typeof(RegistroFacturacionAnulacion), Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd", Order = 0)]
    public required object Item { get; set; }
}
