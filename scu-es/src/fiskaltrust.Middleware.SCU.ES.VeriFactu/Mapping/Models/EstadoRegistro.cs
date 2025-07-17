using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/RespuestaSuministro.xsd")]
public enum EstadoRegistro
{
    Correcto,
    AceptadoConErrores,
    Incorrecto,
}