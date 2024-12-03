using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.Soap;

[Serializable]
[XmlRoot("Body")]
public class RequestBody
{
    public const string NAMESPACE = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroLR.xsd";

    [XmlElement(Namespace = NAMESPACE)]
    public required RegFactuSistemaFacturacion RegFactuSistemaFacturacion { get; set; }
}
