// Hand-written extension of the xsd.exe-generated model in response-v2.0.2.cs.
// Lives in its own file so that regenerating response-v2.0.2.cs never touches it.
//
// xsd.exe collapses the PackingsDeclaration complexType (TransportTypes-v2.0.2.xsd: a sequence of
// one unbounded 'Packages' element) into a plain array and, because 'packingsDeclarations' is
// itself unbounded, emits a jagged PackagingDetailType[][] annotated with a single-level
// [XmlArrayItem]. XmlSerializer cannot build that member, so the generated property is
// [XmlIgnore]d (see Scheme/README.md) and this file provides the wrapper type xsd.exe should
// have generated, plus a serializable replacement property.

/// <summary>
/// PackingsDeclaration (TransportTypes-v2.0.2.xsd): one declaration holding 1..n packages.
/// </summary>
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.aade.gr/myDATA/invoice/v1.0")]
public partial class PackingsDeclaration
{
    [System.Xml.Serialization.XmlElementAttribute("Packages")]
    public PackagingDetailType[]? Packages { get; set; }
}

public partial class AadeBookInvoiceType
{
    /// <summary>
    /// Serialized as the XSD's 'packingsDeclarations' element. Use this instead of the generated,
    /// [XmlIgnore]d 'packingsDeclarations' member.
    /// </summary>
    /// <remarks>
    /// XmlSerializer writes members in compile order; the csproj compiles this file last, so this
    /// element is written after all generated members — i.e. after 'invoiceDeliveryStatus' and
    /// 'deliveryLifecycle' rather than before them as the XSD sequence requires. Both of those are
    /// populated by AADE only and never set by this SCU, so submitted documents remain
    /// schema-valid.
    /// </remarks>
    [System.Xml.Serialization.XmlElementAttribute("packingsDeclarations")]
    public PackingsDeclaration[]? PackingsDeclarations { get; set; }
}
