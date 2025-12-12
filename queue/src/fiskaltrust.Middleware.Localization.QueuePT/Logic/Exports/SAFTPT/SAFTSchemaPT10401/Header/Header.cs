using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;

[XmlRoot(ElementName = "Header")]
public class Header
{
    /// <summary>
    /// The version of XML scheme to be used is the one available on http://www.portaldasfinancas.gov.pt 
    /// String
    /// Max-length 10
    /// Required
    /// </summary>
    [XmlElement(ElementName = "AuditFileVersion")]
    [MaxLength(10)]
    [Required]
    public required string AuditFileVersion { get; set; }

    /// <summary>
    /// It is obtained by linking together the name of the commercial registry office and the commercial registration number, separated by a space.
    /// When there is no commercial registration, the Tax Registration Number shall be inserted.
    /// String
    /// Max-length 50
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CompanyID")]
    [MaxLength(50)]
    [Required]
    public required string CompanyID { get; set; }

    /// <summary>
    /// To be filled in with the Portuguese Tax Identification Number/Tax Registration Number without spaces and without country prefixes.
    /// Integer
    /// Max-length 9
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxRegistrationNumber")]
    [MaxLength(9)]
    [Required]
    public required int TaxRegistrationNumber { get; set; }

    /// <summary>
    /// Shall be filled in with the type of program, indicating the applicable data (including the transport documents, conference documents and issued receipts, if any):
    /// "C" - Accounting;
    /// "E" - Invoices issued by third parties;
    /// "F" - Invoicing;
    /// "I" - Invoicing and accounting integrated data;
    /// "P" - Invoicing partial data.
    /// "R" - Receipts (a);
    /// "S" - Self-billing;
    /// "T" - Transport documents (a).
    /// 
    /// (a) Type of program should be indicated, in case only this type of documents are issued. If not, fill in with type “C”, “F” or “I”.
    /// 
    /// Max-length
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxAccountingBasis")]
    [MaxLength(1)]
    [Required]
    public required TaxAccountingBasis TaxAccountingBasis { get; set; }

    /// <summary>
    /// Social designation of the company or taxpayer’s name.
    ///
    /// Max-length 100
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CompanyName")]
    [MaxLength(100)]
    [Required]
    public required string CompanyName { get; set; }

    /// <summary>
    /// Commercial designation of the taxpayer.
    ///
    /// Max-length 60
    /// </summary>
    [XmlElement(ElementName = "BusinessName")]
    [MaxLength(60)]
    public string? BusinessName { get; set; }

    /// <summary>
    /// Social designation of the company or taxpayer’s name.
    /// 
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CompanyAddress")]
    [Required]
    public required CompanyAddress CompanyAddress { get; set; }

    /// <summary>
    /// Use Corporate Income Tax Code rules, in the case of accounting periods that do not coincide with the calendar year. 
    /// 
    /// (E.g. taxation period from 01.10.2012 to 30.09.2013 corresponds to the Fiscal year = 2012).
    /// 
    /// Max-length 4
    /// Required
    /// </summary>
    [XmlElement(ElementName = "FiscalYear")]
    [Required]
    public required int FiscalYear { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Required
    /// </summary>
    [XmlIgnore]
    [Required]
    public required DateTime StartDate { get; set; }

    [XmlElement(ElementName = "StartDate")]
    public string StartDateString
    { 
        get => StartDate.ToString("yyyy-MM-dd"); 
        set => StartDate = DateTime.Parse(value);
    }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Required
    /// </summary>
    [XmlIgnore]
    [Required]
    public required DateTime EndDate { get; set; }

    [XmlElement(ElementName = "EndDate")]
    public string EndDateString
    { 
        get => EndDate.ToString("yyyy-MM-dd"); 
        set => EndDate = DateTime.Parse(value);
    }

    /// <summary>
    /// Identifies the default currency to use in the monetary type fields in the file.
    ///  
    /// Fill in with "EUR".
    /// 
    /// Required
    /// </summary>
    [XmlElement(ElementName = "CurrencyCode")]
    [Required]
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Date of creation of file XML of SAF-T (PT)
    /// 
    /// Required
    /// </summary>
    [XmlIgnore]
    [Required]
    public required DateTime DateCreated { get; set; }

    [XmlElement(ElementName = "DateCreated")]
    public string DateCreatedString
    { get => DateCreated.ToString("yyyy-MM-dd");

        set => DateCreated = DateTime.Parse(value);
    }

    /// <summary>
    /// In the case of an invoicing file, it shall be specified which establishment the produced file refers to, if applicable, otherwise it must be filled in with the specification "Global".
    /// 
    /// In the case of an accounting file or integrated file this field must be filled in with the specification "Sede".
    /// 
    /// Max-length: 20
    /// Required
    /// </summary>
    [XmlElement(ElementName = "TaxEntity")]
    [MaxLength(20)]
    [Required]
    public required string TaxEntity { get; set; }

    /// <summary>
    /// Fill in with the Tax Identification Number/Tax Registration Number of the entity that produced the software.
    /// 
    /// Max-length: 20
    /// Required
    /// </summary>
    [XmlElement(ElementName = "ProductCompanyTaxID")]
    [MaxLength(20)]
    [Required]
    public required string ProductCompanyTaxID { get; set; }

    /// <summary>
    /// Number of the software certificate allocated to the entity that created the software, pursuant to Ordinance No. 363/2010, of 23th June.
    ///
    /// If it doesn’t apply, the field must be filled in with "0" (zero).
    /// </summary>
    [XmlElement(ElementName = "SoftwareCertificateNumber")]
    [Required]
    public required string SoftwareCertificateNumber { get; set; }

    /// <summary>
    /// Name of the product that generates the SAF-T (PT).
    /// 
    /// The commercial name of the software as well as the name of the company that produced it shall be indicated in the format "Product name/company name".
    /// 
    /// Max-length: 255
    /// Required
    /// </summary>
    [XmlElement(ElementName = "ProductID")]
    [MaxLength(255)]
    [Required]
    public required string ProductID { get; set; }

    /// <summary>
    /// The product version shall be indicated
    /// 
    /// Max-length: 30
    /// Required
    /// </summary>
    [XmlElement(ElementName = "ProductVersion")]
    [MaxLength(30)]
    public required string ProductVersion { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Max-length: 255
    /// </summary>
    [XmlElement(ElementName = "HeaderComment")]
    [MaxLength(255)]
    public string? HeaderComment { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Max-length: 20
    /// </summary>
    [XmlElement(ElementName = "Telephone")]
    [MaxLength(20)]
    public string? Telephone { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Max-length: 20
    /// </summary>
    [XmlElement(ElementName = "Fax")]
    public string? Fax { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Max-length: 254
    /// </summary>
    [XmlElement(ElementName = "Email")]
    public string? Email { get; set; }

    /// <summary>
    /// TODO: Add description
    /// 
    /// Max-length: 60
    /// </summary>
    [XmlElement(ElementName = "Website")]
    public string? Website { get; set; }
}

