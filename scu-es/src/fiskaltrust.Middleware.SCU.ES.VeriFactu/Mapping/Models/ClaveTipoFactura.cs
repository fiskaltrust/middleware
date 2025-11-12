using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public enum ClaveTipoFactura
{
    F1,
    F2,
    R1,
    R2,
    R3,
    R4,
    R5,
    F3,
}