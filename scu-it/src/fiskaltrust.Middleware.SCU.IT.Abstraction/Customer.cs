namespace fiskaltrust.Middleware.SCU.IT.Abstraction;

public class Customer
{
    public string? CustomerName { get; set; }

    /// <summary>
    /// No longer read by the Italian SCUs: the codice fiscale moved to <see cref="CustomerTaxId"/>.
    /// Kept on the model so that a PoS still sending it does not fail deserialization.
    /// </summary>
    public string? CustomerId { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerStreet { get; set; }
    public string? CustomerZip { get; set; }
    public string? CustomerCity { get; set; }
    public string? CustomerCountry { get; set; }
    public string? CustomerVATId { get; set; }

    /// <summary>The codice fiscale of the customer.</summary>
    public string? CustomerTaxId { get; set; }
}


public class ReceiptCaseLotteryData
{
    public servizi_lotteriadegliscontrini_gov_it? servizi_lotteriadegliscontrini_gov_it { get; set; }
}

public class servizi_lotteriadegliscontrini_gov_it
{
    public string? codicelotteria { get; set; }
}
