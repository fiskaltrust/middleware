using System;
using System.Collections.Generic;
using System.Text;

#nullable disable

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;

[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlType(AnonymousType = true, Namespace = "urn:ticketbai:emision")]
[System.Xml.Serialization.XmlRoot(Namespace = "urn:ticketbai:emision", IsNullable = false)]
public class TicketBaiResponse
{
    [System.Xml.Serialization.XmlElement(Namespace = "")]
    public Salida Salida { get; set; }
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlType(AnonymousType = true)]
[System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
public partial class Salida
{
    public string IdentificadorTBAI { get; set; }

    public string FechaRecepcion { get; set; }

    public string Estado { get; set; }

    public string Descripcion { get; set; }

    public string Azalpena { get; set; }

    public string CSV { get; set; }
}