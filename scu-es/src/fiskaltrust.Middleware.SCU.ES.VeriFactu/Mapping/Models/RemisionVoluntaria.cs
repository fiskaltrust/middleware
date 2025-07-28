using System.Xml.Serialization;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuModels;

[XmlType(AnonymousType = true, Namespace = "https://www2.agenciatributaria.gob.es/static_files/common/internet/dep/aplicaciones/es/aeat/tike/cont/ws/SuministroInformacion.xsd")]
public class RemisionVoluntaria
{
    [XmlElement(Order = 0)]
    public string? FechaFinVeriFactu { get; set; }
    [XmlIgnore]
    public bool FechaFinVeriFactuSpecified => FechaFinVeriFactu is not null;

    [XmlElement(Order = 1)]
    public Booleano? Incidencia { get; set; }
    [XmlIgnore]
    public bool IncidenciaSpecified => Incidencia is not null;
}