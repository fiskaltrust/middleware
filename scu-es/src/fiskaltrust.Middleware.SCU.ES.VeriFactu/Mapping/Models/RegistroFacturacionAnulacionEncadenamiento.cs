using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Models;

[XmlType(AnonymousType = true, Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RegistroFacturacionAnulacionEncadenamiento
{
    [XmlElement("PrimerRegistro", typeof(PrimerRegistroCadena), Order = 0)]
    [XmlElement("RegistroAnterior", typeof(EncadenamientoFacturaAnterior), Order = 0)]
    public required object Item { get; set; }
}