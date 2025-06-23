using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping.Models;

[XmlType(Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class Operacion
{
    [XmlElement(Order = 0)]
    public required TipoOperacion TipoOperacion { get; set; }

    [XmlElement(Order = 1)]
    public Booleano? Subsanacion { get; set; }
    [XmlIgnore]
    public bool SubsanacionSpecified => Subsanacion is not null;

    [XmlElement(Order = 2)]
    public RechazoPrevio? RechazoPrevio { get; set; }
    [XmlIgnore]
    public bool RechazoPrevioSpecified => RechazoPrevio is not null;

    [XmlElement(Order = 3)]
    public Booleano? SinRegistroPrevio { get; set; }
    [XmlIgnore]
    public bool SinRegistroPrevioSpecified => SinRegistroPrevio is not null;
}