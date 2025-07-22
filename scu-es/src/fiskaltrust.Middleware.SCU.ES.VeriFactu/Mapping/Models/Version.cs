
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public enum Version
{
    [XmlEnum("1.0")]
    Item10,
}