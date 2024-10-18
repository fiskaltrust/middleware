using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace fiskaltrust.Middleware.Localization.QueuePT.Models;

public class PTQrCode
{
    public const string CUSTOMER_TIN_ANONYMOUS = "999999990";
    public const string CUSTOMER_COUNTRY_ANONYMOUS = "PT";

    /// <summary>
    /// Fill with the issuer’s TIN without blanks and withoutcountry prefix, according to the TaxRegistrationNumber field of the SAF-T (PT).
    /// </summary>
    public required string IssuerTIN { get; set; }

    /// <summary>
    /// Fill with the customer TIN without country prefix, according to the CustomerTaxID field of the SAF-T (PT). When issuing a document to a “Consumidor final” (Final Consumer) fill with 999999990.
    /// </summary>
    public required string CustomerTIN { get; set; }

    /// <summary>
    /// Fill according to the Country field of the SAF-T (PT) customer table.
    /// </summary>
    public required string CustomerCountry { get; set; }

    /// <summary>
    /// Fill according to the typology of the SAF-T (PT) - InvoiceType, MovementType, WorkType or PaymentType fields.
    /// </summary>
    public required string DocumentType { get; set; }

    /// <summary>
    /// Fill according to the typology of the SAF-T (PT) - InvoiceStatus, MovementStatus, WorkStatus or PaymentStatus fields.
    /// </summary>
    public required string DocumentStatus { get; set; }

    /// <summary>
    /// Use YYYYMMDD format. Corresponds to SAF-T (PT) InvoiceDate, MovementDate, WorkDate or TransactionDate fields without hyphens.
    /// </summary>
    public required DateTime DocumentDate { get; set; }

    /// <summary>
    /// Fill according to the typology of the SAF-T (PT) - InvoiceNo, DocumentNumber or PaymentRefNo fields.
    /// </summary>
    public required string UniqueIdentificationOfTheDocument { get; set; }

    /// <summary>
    /// Fill with the document unique code, according to the ATCUD fields of the SAF-T (PT).
    /// </summary>
    public required string ATCUD { get; set; } // will put 0 right now

    /// <summary>
    /// Fill according to the technicalnotes of the TaxCountryRegion field of SAF-T (PT). In case of a document without an indication of the VAT rate, which must be shown in table 4.2, 4.3 or 4.4 of the SAF-T (PT), fill in with «0» (I1:0)
    /// </summary>
    public required string TaxCountryRegion { get; set; }

    /// <summary>
    /// Total amount of the VAT exempt tax base, including transactions liable to stamp duty (whether or not exempt from stamp duty). Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TaxableBasisOfVAT_ExemptRate { get; set; }

    /// <summary>
    /// Total amount of the tax base subject to the reduced rate of VAT. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TaxableBasisOfVAT_ReducedRate { get; set; }

    /// <summary>
    /// Total amount of VAT at the reduced rate in the document. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TotalVAT_ReducedRate { get; set; }

    /// <summary>
    /// Total amount of the tax base subject to the intermediate rate of VAT. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TaxableBasisOfVAT_IntermediateRate { get; set; }

    /// <summary>
    /// Total amount of VAT at the intermediate rate in the document. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TotalVAT_IntermediateRate { get; set; }

    /// <summary>
    /// Total amount of the tax base subject to the standard rate of VAT. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TaxableBasisOfVAT_StandardRate { get; set; }

    /// <summary>
    /// Total amount of VAT at the standard rate in the document. Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public decimal? TotalVAT_StandardRate { get; set; }

    /// <summary>
    /// Total amount of VAT and Stamp duty - TaxPayable field of SAF-T (PT). Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public required decimal TotalTaxes { get; set; }

    /// <summary>
    /// Total amount of the document– GrossTotal field of SAF-T (PT). Format with two decimal places, with “.” as decimal separator and without separator of thousands.
    /// </summary>
    public required decimal GrossTotal { get; set; }

    /// <summary>
    /// Complete in accordance with Article 6(3)(a) of Ordinance No. 363/2010 of June 23rd.
    /// </summary>
    public required string Hash { get; set; }

    /// <summary>
    /// Fill with the certificate number assigned by the Tax and Customs Authority, according to the SoftwareCertificateNumber field of the SAF-T (PT).
    /// </summary>
    public required string SoftwareCertificateNumber { get; set; }

    /// <summary>Free fill-in field, in which, for example, payment information can be indicated (e.g.: from IBAN or ATM Ref.,with the separator «;»). This field shall not contain the asterisk character (*).
    /// </summary>
    public string? OtherInformation { get; set; }

    public static string CreateCurrencyValue(decimal value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    public string GenerateQRCode()
    {
        var sb = new StringBuilder();
        sb.Append($"A:{IssuerTIN}*");
        sb.Append($"B:{CustomerTIN}*");
        sb.Append($"C:{CustomerCountry}*");
        sb.Append($"D:{DocumentType}*");
        sb.Append($"E:{DocumentStatus}*");
        sb.Append($"F:{DocumentDate:yyyyMMdd}*");
        sb.Append($"G:{UniqueIdentificationOfTheDocument}*");
        sb.Append($"H:{ATCUD}*");

        sb.Append($"I1:{TaxCountryRegion}*");

        if (TaxableBasisOfVAT_ExemptRate.HasValue)
        {
            sb.Append($"I2:{CreateCurrencyValue(TaxableBasisOfVAT_ExemptRate.Value)}*");
        }

        if (TaxableBasisOfVAT_ReducedRate.HasValue)
        {
            sb.Append($"I3:{CreateCurrencyValue(TaxableBasisOfVAT_ReducedRate.Value)}*");
        }

        if (TotalVAT_ReducedRate.HasValue)
        {
            sb.Append($"I4:{CreateCurrencyValue(TotalVAT_ReducedRate.Value)}*");
        }

        if (TaxableBasisOfVAT_IntermediateRate.HasValue)
        {
            sb.Append($"I5:{CreateCurrencyValue(TaxableBasisOfVAT_IntermediateRate.Value)}*");
        }

        if (TotalVAT_IntermediateRate.HasValue)
        {
            sb.Append($"I6:{CreateCurrencyValue(TotalVAT_IntermediateRate.Value)}*");
        }

        if (TaxableBasisOfVAT_StandardRate.HasValue)
        {
            sb.Append($"I7:{CreateCurrencyValue(TaxableBasisOfVAT_StandardRate.Value)}*");
        }

        if (TotalVAT_StandardRate.HasValue)
        {
            sb.Append($"I8:{CreateCurrencyValue(TotalVAT_StandardRate.Value)}*");
        }

        sb.Append($"N:{CreateCurrencyValue(TotalTaxes)}*");
        sb.Append($"O:{CreateCurrencyValue(GrossTotal)}*");
        sb.Append($"Q:{Hash}*");
        sb.Append($"R:{SoftwareCertificateNumber}*");

        if (!string.IsNullOrEmpty(OtherInformation))
        {
            sb.Append($"S:{OtherInformation}");
        }

        return sb.ToString().TrimEnd('*'); // Removes trailing '*'
    }

}