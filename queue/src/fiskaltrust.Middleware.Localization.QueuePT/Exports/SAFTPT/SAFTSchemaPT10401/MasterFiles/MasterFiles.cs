using System.Xml.Serialization;

namespace fiskaltrust.SAFT.CLI.SAFTSchemaPT10401;

[XmlRoot(ElementName = "MasterFiles")]
public class MasterFiles
{
    /*
    The table of the General Ledger to be exported is the one mentioned in the accounting normalization system and other legal provisions in force for the relevant sector of activity.
    The records of accounting classes shall not be exported.
    In case of aggregating accounts containing sub-accounts with debit balances and sub-accounts with credit balances, the debit and credit balances shall be shown in the aggregating account.
    
    [XmlElement(ElementName = "GeneralLedgerAccounts")]
    public object GeneralLedgerAccounts { get; set; } <summary>
    */

    /// <summary>
    /// This table shall contain all the existing records operated during the taxation period in the relevant customers’ file, as well as those which may be implicit in the operations and do not exist in the relevant file. If, for instance, there is a sale for cash showing only the customer’s taxpayer registration number or his name, and not included in the customers file of the application, this client’s data shall be exported as client in the SAF-T (PT).
    /// </summary>
    [XmlElement(ElementName = "Customer")]
    public List<Customer>? Customer { get; set; }

    /*
    This table shall contain all the records operated during the tax period in the relevant database.
    
    [XmlElement(ElementName = "Supplier")]
    public object Supplier { get; set; } <summary>
    */

    /// <summary>
    /// This table shall present the catalogue of products and types of services used in the invoicing system, which have been operated, and also the records, which are implicit in the operations and do not exist in the table of products/services of the application.
    /// If, for instance, there is an invoice with a line of freights that does not exist in the articles’ file of the application, this file shall be exported and represented as a product in the SAF-T (PT).
    /// This table shall also show taxes, tax rates, eco taxes, parafiscal charges mentioned in the invoice and contributing or not to the taxable basis for VAT or Stamp Duty - except VAT and Stamp duty, which shall be showed in 2.5. – TaxTable (Table of taxes).
    /// </summary>
    [XmlElement(ElementName = "Product")]
    public List<Product>? Product { get; set; }

    /// <summary>
    /// This table shows the VAT regimes applied in each fiscal area and the different types of stamp duty to be paid, applicable to the lines of documents recorded in Table 4. – SourceDocuments.
    /// </summary>
    [XmlElement(ElementName = "TaxTable")]
    public TaxTable? TaxTable { get; set; }
}

