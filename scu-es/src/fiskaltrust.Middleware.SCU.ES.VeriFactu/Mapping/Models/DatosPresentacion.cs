using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class DatosPresentacion
{
    [XmlElement(Order = 0)]
    public required string NIFPresentador { get; set; }

    [XmlElement(Order = 1)]
    public required DateTime TimestampPresentacion { get; set; }
}