using System.Xml.Serialization;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.HeaderContracts;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.MasterFileContracts;
using fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401.SourceDocumentContracts;

namespace fiskaltrust.Middleware.Localization.QueuePT.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "AuditFile", Namespace = "urn:OECD:StandardAuditFile-Tax:PT_1.04_01")]
public class AuditFile
{
    [XmlElement(ElementName = "Header")]
    public required Header Header { get; set; }

    /// <summary>
    /// Master Files 2.1, 2.2, 2.3, 2.4 and 2.5 are required under the conditions stated in f), g), h) and i) of paragraph 1 of this Annex.
    /// </summary>
    [XmlElement(ElementName = "MasterFiles")]
    public required MasterFiles MasterFiles { get; set; }

    /// <summary>
    /// Lines without fiscal relevance must not be exported, in particular technical descriptions, installation instructions and guarantee conditions.
    /// 
    /// The internal code of the document type cannot be used in different document types, regardless of the table in which it is to be exported.
    /// </summary>
    [XmlElement(ElementName = "SourceDocuments")]
    public SourceDocuments? SourceDocuments { get; set; }
}

